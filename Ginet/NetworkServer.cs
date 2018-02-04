using Lidgren.Network;
using System;
using Ginet.Logging;
using Ginet.NetPackages;
using System.Threading.Tasks;
using System.Linq;

namespace Ginet
{
    public class NetworkServer : NetworkManager<NetServer>
    {
        public NetworkServer(string serverName, Action<PackageContainerBuilder> packageContainer, Action<NetPeerConfiguration> configuration, IAppender output = null, bool enableAllIncomingMessages = true)
            : base(serverName, packageContainer, configuration, output, enableAllIncomingMessages)
        {
        }

        public IDisposable Broadcast<TPackage>(Action<TPackage, NetIncomingMessage, NetOutgoingMessage> responder, Action<TPackage> packageTransformer = null)
            where TPackage : class
        {
            return IncomingMessageHandler.OnPackage<TPackage>((msg, im) =>
            {
                packageTransformer?.Invoke(msg);
                responder(msg, im, ConvertToOutgoingMessage(msg));
            });
        }

        public IDisposable BroadcastExceptSender<TPackage>(Action<NetConnection, TPackage> packageTransformer = null)
            where TPackage : class
        {
            return IncomingMessageHandler.OnPackage<TPackage>((msg, im) =>
            {
                packageTransformer?.Invoke(im.SenderConnection, msg);
                SendToAllExcept(ConvertToOutgoingMessage(msg), new[] { im.SenderConnection });
            });
        }

        public IDisposable Broadcast<TPackage>(Func<TPackage, NetIncomingMessage, NetOutgoingMessage, Task> broadcaster, Action<TPackage> packageTransformer = null)
            where TPackage : class
        {
            return IncomingMessageHandler.OnPackage((Action<TPackage, NetIncomingMessage>)((msg, im) =>
            {
                packageTransformer?.Invoke(msg);
                broadcaster(msg, im, ConvertToOutgoingMessage(msg));
            }));
        }

        public void SendToAllExcept(NetOutgoingMessage om, params NetConnection[] excluded)
        {
            var otherConnections = Host.Connections.Where(c => !excluded.Contains(c)).ToArray();
            if (!otherConnections.Any())
            {
                return;
            }
            Host.SendMessage(om, otherConnections, DeliveryMethod, Channel);
        }

        public void SendToAllExcept<TPackage>(TPackage package, params NetConnection[] excluded)
            where TPackage : class
        {
            SendToAllExcept(ConvertToOutgoingMessage(package), excluded);
        }

    }
}
