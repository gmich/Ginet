using Ginet.Infrastructure;
using Ginet.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Ginet.Terminal
{
    public class CommandHost : ITerminal
    {
        private readonly ConcurrentRepository<string, CommandTableEntry> commandTable =
            new ConcurrentRepository<string, CommandTableEntry>();
        private readonly ICommandParser parser;

        public List<IPEndPoint> WhiteList { get; } = new List<IPEndPoint>();

        public CommandHost(ICommandParser parser, IPEndPoint host)
        {
            this.parser = parser;
            WhiteList.Add(host);
            RegisterCommand("help", "get help", args => ExecutionResult.Ok(Help(true)));
        }

        private string Help(bool includeWhiteListed)
        {
            StringBuilder strBuilder = new StringBuilder();
            foreach (var entry in commandTable.Items)
            {
                if (!includeWhiteListed)
                {
                    if (entry.Value.OnlyWhiteListed)
                    {
                        strBuilder.AppendLine($"{entry.Key} - {entry.Value.Description}");
                    }
                }
                else
                {
                    strBuilder.AppendLine($"{entry.Key} - {entry.Value.OnlyWhiteListed} - {entry.Value.Description}");
                }
            }
            return strBuilder.ToString();
        }

        public IDisposable RegisterCommand(string command, string description, CommandDelegate callback)
        {
            Contract.Requires(!string.IsNullOrEmpty(command));
            Contract.Requires(!string.IsNullOrEmpty(description));
            Contract.Requires(callback != null);

            return commandTable.Add(command,
                new CommandTableEntry
                {
                    Description = description,
                    Callback = callback
                });
        }

        private class CallBackInfo
        {
            public string Command { get; set; }
            public Func<IEnumerable<string>, ExecutionResult> CallBack { get; set; }
            public CommandInfo.ContinuationOption Continuation { get; set; }
            public IEnumerable<string> Arguments { get; set; }
        }

        public ExecutionResult ExecuteCommand(string text, IPEndPoint sender)
        {
            var commands = parser.Parse(text);
            var callBacks = new List<CallBackInfo>();

            foreach (var commandInfo in commands)
            {
                if (commandTable.HasKey(commandInfo.Command))
                {
                    var cmdEntry = commandTable[commandInfo.Command];
                    if (cmdEntry.OnlyWhiteListed)
                    {
                        if (!WhiteList.Contains(sender))
                        {
                            return ExecutionResult.Faulted(ExecutionResult.Status.Unauthorized,
                                $"Unable to execute {commandInfo.Command}. Not white listed");
                        }
                    }
                    callBacks.Add(new CallBackInfo
                    {
                        Command = commandInfo.Command,
                        Arguments = commandInfo.Arguments,
                        CallBack = args => commandTable[commandInfo.Command].Callback(args),
                        Continuation = commandInfo.Continuation
                    });
                }
                else
                {
                    return ExecutionResult.Faulted(ExecutionResult.Status.CommandNotFound,
                        $"{commandInfo.Command} is not a registered command");
                }
            }

            var callbackResult = ExecutionResult.Faulted(ExecutionResult.Status.Faulted, "Nothing to execute");
            for (int callbackCounter = 0; callbackCounter < callBacks.Count; callbackCounter++)
            {
                try
                {
                    callbackResult = callBacks[callbackCounter].CallBack.Invoke(callBacks[callbackCounter].Arguments);
                }
                catch (Exception ex)
                {
                    return ExecutionResult.Faulted(ExecutionResult.Status.Exception,
                        $"{callBacks[callbackCounter].Command} execution threw an exception. {ex.Message}");
                }
                if (!callbackResult.Successful)
                {
                    return callbackResult;
                }
                switch (callBacks[callbackCounter].Continuation)
                {
                    case CommandInfo.ContinuationOption.Flush:
                        break;
                    case CommandInfo.ContinuationOption.Append:
                        if (callbackCounter + 1 < callBacks.Count)
                        {
                            callBacks[callbackCounter + 1].Arguments = new[] { callbackResult.Result };
                        }
                        break;
                }
            }
            return callbackResult;
        }
    }
}
