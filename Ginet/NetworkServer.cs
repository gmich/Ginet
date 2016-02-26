using Lidgren.Network;
using System;
using Ginet.Logging;
using Ginet.NetPackages;
using System.Threading.Tasks;
using System.Linq;
using Ginet.Terminal;

namespace Ginet
{
    public class NetworkServer : NetworkManager<NetServer>
    {
        public NetworkServer(string name, Action<GinetConfig> configuration, Action<PackageContainerBuilder> containerBuilder)
            : base(name, configuration, containerBuilder)
        {
            Terminal.RegisterCommand("show-clients", "shows all connected clients", args =>
                  ExecutionResult.Ok(String.Concat(Host.Connections.Select(c => c.ToString() + Environment.NewLine))),
                  ExecutionOptions.None);
        }

        public void Start()
        {
            StartHost();
        }

        public void SendCommand(Command command, NetConnection recipient)
        {
            Send(command, (om, client) =>
                client.SendMessage(om, recipient, Configuration.DeliveryMethod));
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
                SendToAllExcept(ConvertToOutgoingMessage(msg), im.SenderConnection);
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

        public void SendToAllExcept(NetOutgoingMessage om, NetConnection excluded)
        {
            var otherConnections = Host.Connections.Where(c => c != excluded).ToArray();
            if (!otherConnections.Any())
            {
                return;
            }
            Host.SendMessage(om, otherConnections, Configuration.DeliveryMethod, Configuration.DefaultChannel);
        }

        public void SendToAllExcept<TPackage>(TPackage package, NetConnection excluded)
            where TPackage : class
        {
            SendToAllExcept(ConvertToOutgoingMessage(package), excluded);
        }

    }
}
