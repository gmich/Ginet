using System;
using System.Collections.Generic;
using Ginet.Logging;
using System.Threading.Tasks;
using System.Net;

namespace Ginet.Terminal
{
    public interface ITerminal
    {
        List<IPEndPoint> WhiteList { get; }
        ExecutionResult ExecuteCommand(string text, IPEndPoint sender);
        IDisposable RegisterCommand(string command, string description, CommandDelegate callback);
    }
}