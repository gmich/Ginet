using System;
using System.Threading.Tasks;
using Ginet.NetPackages;
using Lidgren.Network;
using Ginet.Terminal;

namespace Ginet
{
    public interface INetworkManager<TPeer>
        where TPeer : NetPeer
    {
        GinetConfig Configuration { get; }
        IncomingMessageHandler IncomingMessageHandler { get; }
        TPeer Host { get; }
        ITerminal Terminal { get; }
        void ExecuteCommand(string text);
        IPackageSender LiftSender(Action<NetOutgoingMessage, TPeer> sender);
        NetOutgoingMessage ConvertToOutgoingMessage<TPackage>(TPackage package)
         where TPackage : class;
        Task ProcessMessages();
        void ProcessMessagesInBackground();
        void Send<TMessage>(TMessage message, Action<NetOutgoingMessage, TPeer> sender) where TMessage : class;
        void Stop(string disconnectMsg);
    }
}