using Ginet.Infrastructure;
using Ginet.Logging;
using Ginet.NetPackages;
using Ginet.Terminal;
using Lidgren.Network;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Ginet
{
    public abstract class NetworkManager<TNetPeer> : INetworkManager<TNetPeer>
        where TNetPeer : NetPeer
    {
        private readonly PackageContainer container;
        private ParallelTaskStarter asyncMessageProcessor;
        private readonly IPEndPoint localAddress;

        public TNetPeer Host { get; }
        public IncomingMessageHandler IncomingMessageHandler { get; }
        public IAppender Out { get; }
        public GinetConfig Configuration { get; }
        public ITerminal Terminal { get; }

        public NetworkManager(string name, Action<GinetConfig> configuration, Action<PackageContainerBuilder> containerBuilder)
        {
            Configuration = new GinetConfig
            {
                NetConfig = new NetPeerConfiguration(name)
            };
            if (Configuration.Output != null)
            {
                GinetOut.Appender = Out;
            }
            configuration(Configuration);

            Out = GinetOut.Appender[GetType().FullName];
            var builder = new PackageContainerBuilder();
            builder.Register<Command>();
            containerBuilder(builder);

            if (Configuration.EnableAllIncomingMessages)
            {
                Configuration.NetConfig.EnableMessageType(NetIncomingMessageType.Data);
                Configuration.NetConfig.EnableMessageType(NetIncomingMessageType.NatIntroductionSuccess);
                Configuration.NetConfig.EnableMessageType(NetIncomingMessageType.DiscoveryResponse);
                Configuration.NetConfig.EnableMessageType(NetIncomingMessageType.Receipt);
                Configuration.NetConfig.EnableMessageType(NetIncomingMessageType.StatusChanged);
                Configuration.NetConfig.EnableMessageType(NetIncomingMessageType.UnconnectedData);
                Configuration.NetConfig.EnableMessageType(NetIncomingMessageType.WarningMessage);
                Configuration.NetConfig.EnableMessageType(NetIncomingMessageType.VerboseDebugMessage);
                Configuration.NetConfig.EnableMessageType(NetIncomingMessageType.ErrorMessage);
                Configuration.NetConfig.EnableMessageType(NetIncomingMessageType.Error);
                Configuration.NetConfig.EnableMessageType(NetIncomingMessageType.DebugMessage);
                Configuration.NetConfig.EnableMessageType(NetIncomingMessageType.ConnectionApproval);
                Configuration.NetConfig.EnableMessageType(NetIncomingMessageType.DiscoveryRequest);
                //config.EnableMessageType(NetIncomingMessageType.ConnectionLatencyUpdated);
            }
            Host = (TNetPeer)Activator.CreateInstance(typeof(TNetPeer), new object[] { Configuration.NetConfig });
            container = builder.Build();
            IncomingMessageHandler = new IncomingMessageHandler(container);
            localAddress = new IPEndPoint(
                Configuration.NetConfig.LocalAddress,
                Configuration.NetConfig.Port);

            Terminal = new CommandHost(new CommandParser(), localAddress);
        }

        public void ExecuteCommand(string command)
        {
            var res = Terminal.ExecuteCommand(command, localAddress);
            Configuration.TerminalOutput(res.ToString());
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
            var id = container.GetIdFromType(typeof(TPackage));
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
        protected void StartHost()
        {
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