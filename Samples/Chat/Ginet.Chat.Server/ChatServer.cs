using Ginet.Chat.Packages;
using Lidgren.Network;
using System.Reflection;
using System.Threading.Tasks;

namespace Ginet.Chat.Server
{
    internal class ChatServer
    {
        private readonly NetworkServer server;
        private readonly string password = "1234";

        public void ExecuteCommand(string cmd)
        {
            server.ExecuteCommand(cmd);
        }

        public ChatServer()
        {
            server = new NetworkServer("Chat",
            cfg =>
            {
                cfg.NetConfig.Port = 1111;
                cfg.NetConfig.ConnectionTimeout = 5.0f;
                cfg.DeliveryMethod = NetDeliveryMethod.ReliableOrdered;
            },
            container =>
            {
                container.RegisterPackages(Assembly.Load("Ginet.Chat.Packages"));
            });
            server.IncomingMessageHandler.LogTraffic();
            server.IncomingMessageHandler.OnMessage(
                    NetIncomingMessageType.ConnectionApproval, im =>
                        ConfigureConnectionApproval(server.ReadAs<ConnectionApprovalMessage>(im), im));

            ConfigureConnectionChange();
            ConfigureResponses();
        }

        private void ConfigureConnectionChange()
        {
            server.IncomingMessageHandler.OnConnectionChange(NetConnectionStatus.Disconnected, im =>
                server.SendToAllExcept(new ServerNotification
                {
                    Message = $"Disconnected {im.SenderEndPoint}"
                },
                im.SenderConnection)
            );
        }

        private void ConfigureResponses()
        {
            server.BroadcastExceptSender<ChatMessage>((sender, msg) =>
                server.Out.Info($"Received {msg.Message} from {sender}")
            );
        }

        private void ConfigureConnectionApproval(ConnectionApprovalMessage msg, NetIncomingMessage im)
        {
            if (msg.Password == password)
            {
                im.SenderConnection.Approve();
                server.Out.Info($"Approved {msg.Sender} with Endpoint: {im.SenderEndPoint}");
                im.SenderConnection.Tag = msg.Sender;
                server.SendToAllExcept(server.ConvertToOutgoingMessage(
                    new ServerNotification { Message = $"Say welcome to {msg.Sender}" }),
                    im.SenderConnection);
            }
            else
            {
                im.SenderConnection.Deny();
                server.Out.Info($"Denied {msg.Sender} : {im.SenderEndPoint}");
            }
        }

        public void Start()
        {
            server.Start();
            server.ProcessMessagesInBackground();
        }

        public void Stop()
        {
            server.Stop("bye");
        }
    }
}
