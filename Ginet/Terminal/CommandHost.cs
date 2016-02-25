using Ginet.Infrastructure;
using Ginet.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Text;
using System.Threading.Tasks;

namespace Ginet.Terminal
{
    public class CommandHost : ITerminal
    {
        private readonly ConcurrentRepository<string, CommandTableEntry> commandTable =
            new ConcurrentRepository<string, CommandTableEntry>();
        private readonly ICommandParser parser;

        public IAppender Output { get; set; }
        public List<string> WhiteList { get; } = new List<string>();

        public CommandHost(ICommandParser parser, IAppender output)
        {
            this.parser = parser;
            Output = output;
            WhiteList.Add("localhost");
            RegisterCommand("help", "get help", (args) => ExecutionResult.Ok(Help(true)));
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
            public Func<IEnumerable<string>, ExecutionResult> CallBack { get; set; }
            public CommandInfo.ContinuationOption Continuation { get; set; }
            public IEnumerable<string> Arguments { get; set; }
        }

        public Task<ExecutionResult> ExecuteCommand(string text, string sender = "localhost")
        {
            return Task.Run(() =>
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
                    callbackResult = callBacks[callbackCounter].CallBack.Invoke(callBacks[callbackCounter].Arguments);

                    if (!callbackResult.Successful)
                    {
                        return callbackResult;
                    }
                    switch (callBacks[callbackCounter].Continuation)
                    {
                        case CommandInfo.ContinuationOption.Flush:
                            Output.Info(callbackResult.Result);
                            break;
                        case CommandInfo.ContinuationOption.Append:
                            if (callbackCounter + 1 <= callBacks.Count)
                            {
                                callBacks[callbackCounter + 1].Arguments = new[] { callbackResult.Result };
                            }
                            else
                            {
                                Output.Info(callbackResult.Result);
                            }
                            break;
                    }
                }
                return callbackResult;
            });
        }

    }
}
