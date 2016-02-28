using System;
using System.Collections.Generic;
using System.Net;

namespace Ginet.Terminal
{
    public interface ITerminal
    {
        List<IPEndPoint> WhiteList { get; }
        ExecutionResult ExecuteCommand(string text, IPEndPoint sender);
        IDisposable RegisterCommand(string command, string briefDescription, CommandDelegate callback, ExecutionOptions options, Action<CommandBuilder> build = null);
    }
}