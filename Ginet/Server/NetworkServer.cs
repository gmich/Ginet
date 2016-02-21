using Lidgren.Network;
using System;
using Ginet.Logging;
using Ginet.NetPackages;
using System.Threading.Tasks;
using System.Linq;

namespace Ginet.Server
{
    public class NetworkServer : NetworkManager<NetServer>
    {
        public NetworkServer(string serverName, Action<NetPeerConfiguration> configuration, IAppender output = null, bool enableAllIncomingMessages = true)
            : base(serverName, configuration, output, enableAllIncomingMessages)
        {
        }

        public void RespondTo<TPackage>(Action<TPackage, NetIncomingMessage, NetOutgoingMessage> responder)
            where TPackage : class
        {           
            IncomingMessageHandler.OnPackage<TPackage>((msg, im) =>
                    responder(msg, im, ConvertToOutgoingMessage(msg)));

        }
        public void RespondTo<TPackage>(Func<TPackage, NetIncomingMessage, NetOutgoingMessage, Task> responder)
            where TPackage : class
        {
            IncomingMessageHandler.OnPackage<TPackage>((msg, im) =>
                    responder(msg, im, ConvertToOutgoingMessage(msg)));
        }

        public void SendToAllExcept(NetOutgoingMessage om, NetConnection excluded, NetDeliveryMethod deliveryMethod, int channel)
        {
            var otherConnections = Host.Connections.Where(c => c != excluded).ToArray();
            if (otherConnections.Count() == 0)
            {
                return;
            }
            Host.SendMessage(om, otherConnections, deliveryMethod, channel);
        }


    }
}
