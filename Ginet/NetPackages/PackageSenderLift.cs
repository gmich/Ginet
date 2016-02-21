using Ginet.Server;
using Lidgren.Network;
using System;

namespace Ginet.NetPackages
{
    internal class PackageSenderLift<TNetPeer> : IPackageSender
        where TNetPeer : NetPeer
    {
        private readonly Action<NetOutgoingMessage, TNetPeer> sender;
        private readonly INetworkManager<TNetPeer> peer;

        public PackageSenderLift(INetworkManager<TNetPeer> peer, Action<NetOutgoingMessage, TNetPeer> sender)
        {
            this.peer = peer;
            this.sender = sender;
        }

        public void Send<TPackage>(TPackage message)
            where TPackage : class
        {
            peer.Send(message, sender);
        }
    }
}
