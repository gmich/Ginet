﻿using Ginet.Infrastructure;
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

            RegisterCommand("help", "Displays a brief description of the command table", args => 
                ExecutionResult.Ok(
                    GetHelp()),
                ExecutionOptions
                .Everyone
                .WithValidator(CommandValidator.ZeroArguments));

            new CommonCommands(this);
        }

        private string GetHelp()
        {
            StringBuilder strBuilder = new StringBuilder();
            foreach (var entry in commandTable.Items)
            {
                    strBuilder.AppendLine($"{entry.Key} - {entry.Value.Options.OnlyWhiteListed} - {entry.Value.Description}");
                
            }
            return strBuilder.ToString();
        }

        public IDisposable RegisterCommand(string command, string description, CommandDelegate callback, ExecutionOptions options)
        {
            Contract.Requires(!string.IsNullOrEmpty(command));
            Contract.Requires(!string.IsNullOrEmpty(description));
            Contract.Requires(callback != null);

            return commandTable.Add(command,
                new CommandTableEntry
                {
                    Description = description,
                    Callback = callback,
                    Options = options
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
                    if (cmdEntry.Options.OnlyWhiteListed)
                    {
                        if (!WhiteList.Contains(sender))
                        {
                            return ExecutionResult.Faulted(ExecutionResult.Status.Unauthorized,
                                $"Unable to execute {commandInfo.Command}. Not white listed");
                        }
                    }
                    if (cmdEntry.Options.Validator != null)
                    {
                        var validationResult = cmdEntry.Options.Validator.Validate(commandInfo.Arguments);
                        if (!validationResult.Success)
                        {
                            return ExecutionResult.Faulted(ExecutionResult.Status.WrongCommandFormat,
                                $"Validation for command {commandInfo.Command} failed. {validationResult.Message}");
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
