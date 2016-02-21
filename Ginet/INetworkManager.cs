using System;
using System.Threading.Tasks;
using Ginet.Packages;
using Lidgren.Network;

namespace Ginet
{
    public interface INetworkManager<TPeer> 
        where TPeer : NetPeer
    {
        IncomingMessageHandler IncomingMessageHandler { get; }
        PackageConfigurator PackageConfigurator { get; }
        TPeer Host { get; }
        IPackageSender LiftSender(Action<NetOutgoingMessage, TPeer> sender);
        NetOutgoingMessage ConvertToOutgoingMessage<TPackage>(TPackage package)
         where TPackage : class;
        Task ProcessMessages();
        void ProcessMessagesInBackground();
        void Send<TMessage>(TMessage message, Action<NetOutgoingMessage, TPeer> sender) where TMessage : class;
        void Stop(string disconnectMsg);
    }
}