using System;
using System.Collections.Generic;
using Ginet.Logging;
using System.Threading.Tasks;

namespace Ginet.Terminal
{
    public interface ITerminal
    {
        IAppender Output { get; set; }
        List<string> WhiteList { get; }
        Task<ExecutionResult> ExecuteCommand(string text, string sender = "localhost");
        IDisposable RegisterCommand(string command, string description, CommandDelegate callback);
    }
}