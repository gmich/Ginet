using Ginet.Async;
using Ginet.Logging;
using Ginet.Packages;
using Lidgren.Network;
using System;
using System.Threading.Tasks;

namespace Ginet
{
    public abstract class NetworkManager<TNetPeer> : INetworkManager<TNetPeer>
        where TNetPeer : NetPeer
    {
        public TNetPeer Host { get; }
        public IncomingMessageHandler IncomingMessageHandler { get; }
        public PackageConfigurator PackageConfigurator { get; }

        public IAppender Out { get; }

        private ParallelTaskStarter asyncMessageProcessor;

        public NetworkManager(string serverName, Action<NetPeerConfiguration> configuration, IAppender output = null, bool enableAllIncomingMessages = true)
        {
            if (Out != null)
            {
                GinetOut.Appender = Out;
            }
            PackageConfigurator = new PackageConfigurator();
            Out = GinetOut.Appender[GetType().FullName];
            var config = new NetPeerConfiguration(serverName);

            configuration(config);

            if (enableAllIncomingMessages)
            {
                config.EnableMessageType(NetIncomingMessageType.Data);
                config.EnableMessageType(NetIncomingMessageType.WarningMessage);
                config.EnableMessageType(NetIncomingMessageType.VerboseDebugMessage);
                config.EnableMessageType(NetIncomingMessageType.ErrorMessage);
                config.EnableMessageType(NetIncomingMessageType.Error);
                config.EnableMessageType(NetIncomingMessageType.DebugMessage);
                config.EnableMessageType(NetIncomingMessageType.ConnectionApproval);
                config.EnableMessageType(NetIncomingMessageType.DiscoveryRequest);
            }
            Host = (TNetPeer)Activator.CreateInstance(typeof(TNetPeer), new object[] { config });
            IncomingMessageHandler = new IncomingMessageHandler(id => PackageConfigurator[id]);
        }

        public IPackageSender LiftSender(Action<NetOutgoingMessage, TNetPeer> sender)
        {
            return new PackageSenderLift<TNetPeer>(this, sender);
        }

        public void Send<TPackage>(TPackage message, Action<NetOutgoingMessage, TNetPeer> sender)
            where TPackage : class
        {
            sender(ConvertToOutgoingMessage(message), Host);
        }

        public TPackage ReadAs<TPackage>(NetIncomingMessage im)
        {
            var package = PackageConfigurator[im.ReadString()];
            if (package == null)
            {
                Out.Error($"Unregistered package {typeof(TPackage)}");
            }
            return (TPackage)package.Serializer.Decode(im, package.Type);
        }

        public NetOutgoingMessage ConvertToOutgoingMessage<TPackage>(TPackage package)
            where TPackage : class
        {
            var id = typeof(TPackage).FullName;
            var om = Host.CreateMessage();
            om.Write(id);

            var entry = PackageConfigurator[id];
            if (entry == null)
            {
                throw new Exception("Package not registered");
                //PackageConfigurator.DefaultSerializer.Encode(message, om);
            }
            entry.Serializer.Encode(package, om);

            return om;
        }

        public void Stop(string disconnectMsg)
        {
            Host.Shutdown(disconnectMsg);
            asyncMessageProcessor?.Stop();
        }

        public async Task ProcessMessages()
        {
            await IncomingMessageHandler.ProcessNetworkMessages(Host);
        }

        public void ProcessMessagesInBackground()
        {
            asyncMessageProcessor = new ParallelTaskStarter(TimeSpan.Zero);
            asyncMessageProcessor.Start(async () => await IncomingMessageHandler.ProcessNetworkMessages(Host));
        }
    }

}