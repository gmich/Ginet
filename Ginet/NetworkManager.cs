using Ginet.Infrastructure;
using Ginet.Logging;
using Ginet.NetPackages;
using Lidgren.Network;
using System;
using System.Threading.Tasks;

namespace Ginet
{
    public abstract class NetworkManager<TNetPeer> : INetworkManager<TNetPeer>
        where TNetPeer : NetPeer
    {
        private ParallelTaskStarter asyncMessageProcessor;
        private readonly PackageContainer container;

        public TNetPeer Host { get; }
        public IncomingMessageHandler IncomingMessageHandler { get; }
        public IAppender Out { get; }
        public int Channel { get; set; }
        public NetDeliveryMethod DeliveryMethod { get; set; }

        public NetworkManager(string serverName, Action<PackageContainerBuilder> containerBuilder, Action<NetPeerConfiguration> configuration, IAppender output = null, bool enableAllIncomingMessages = true)
        {
            if (Out != null)
            {
                GinetOut.Appender = Out;
            }
            Out = GinetOut.Appender[GetType().FullName];
            var config = new NetPeerConfiguration(serverName);
            var builder = new PackageContainerBuilder();
            containerBuilder(builder);

            configuration(config);

            if (enableAllIncomingMessages)
            {
                config.EnableMessageType(NetIncomingMessageType.Data);
                config.EnableMessageType(NetIncomingMessageType.NatIntroductionSuccess);
                config.EnableMessageType(NetIncomingMessageType.DiscoveryResponse);
                config.EnableMessageType(NetIncomingMessageType.Receipt);
                config.EnableMessageType(NetIncomingMessageType.StatusChanged);
                config.EnableMessageType(NetIncomingMessageType.UnconnectedData);
                config.EnableMessageType(NetIncomingMessageType.WarningMessage);
                config.EnableMessageType(NetIncomingMessageType.VerboseDebugMessage);
                config.EnableMessageType(NetIncomingMessageType.ErrorMessage);
                config.EnableMessageType(NetIncomingMessageType.Error);
                config.EnableMessageType(NetIncomingMessageType.DebugMessage);
                config.EnableMessageType(NetIncomingMessageType.ConnectionApproval);
                config.EnableMessageType(NetIncomingMessageType.DiscoveryRequest);
                //config.EnableMessageType(NetIncomingMessageType.ConnectionLatencyUpdated);
            }
            Host = (TNetPeer)Activator.CreateInstance(typeof(TNetPeer), new object[] { config });
            container = builder.Build();
            IncomingMessageHandler = new IncomingMessageHandler(container);
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
            var packageInfo = container.GetPackageInfoFromByte(im.ReadByte());
            if (packageInfo == null)
            {
                string errorMsg = $"The package {typeof(TPackage)} was not registered";
                Out.Error(errorMsg);
                throw new Exception(errorMsg);
            }
            try
            {
                return (TPackage)packageInfo.Serializer.Decode(im, packageInfo.Type);
            }
            catch (Exception ex)
            {
                Out.Error($"Unable to decode {typeof(TPackage)} with {packageInfo.Serializer.GetType()}. {ex.Message}");
                throw ex;
            }
        }

        public NetOutgoingMessage ConvertToOutgoingMessage<TPackage>(TPackage package)
            where TPackage : class
        {
            var id =container.GetIdFromType(typeof(TPackage));
            var om = Host.CreateMessage();
            om.Write(id);

            var packageInfo = container.GetPackageInfoFromByte(id);
            if (packageInfo == null)
            {
                string errorMsg = $"The package {typeof(TPackage)} was not registered";
                Out.Error(errorMsg);
                throw new Exception(errorMsg);
            }
            try
            {
                packageInfo.Serializer.Encode(package, om);
            }
            catch (Exception ex)
            {
                Out.Error($"Unable to encode {typeof(TPackage)} with {packageInfo.Serializer.GetType()}. {ex.Message}");
                throw ex;
            }
            return om;
        }

        public void Stop(string disconnectMsg)
        {
            if (Host.Status == NetPeerStatus.Running)
            {
                Host.Shutdown(disconnectMsg);
                asyncMessageProcessor?.Stop();
            }
            else
            {
                Out.Warn("Unable to stop the server. Server is not running!");
            }
        }

        public void Start(NetDeliveryMethod defaultDeliveryMethod, int defaultChannel)

        {
            DeliveryMethod = defaultDeliveryMethod;
            Channel = defaultChannel;
            try
            {
                Host.Start();
            }
            catch (Exception ex)
            {
                Out.Error($"Unable to connect to start network server. {ex.Message}");
                throw ex;
            }
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