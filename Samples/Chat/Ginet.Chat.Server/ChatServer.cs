using Ginet.Chat.Packages;
using Ginet.Server;
using Lidgren.Network;
using System.Reflection;

namespace Ginet.Chat.Server
{
    internal class ChatServer
    {
        private readonly NetworkServer server;
        private readonly string password = "1234";

        public ChatServer()
        {
            server = new NetworkServer("Chat", cfg =>
            {
                cfg.Port = 1234;
                cfg.ConnectionTimeout = 5.0f;
            });
            server.IncomingMessageHandler.LogTraffic();
            server.PackageConfigurator.RegisterPackages(Assembly.Load("Ginet.Chat.Packages"));
            server.IncomingMessageHandler.OnConnection(NetConnectionStatus.Disconnected, im =>
             {
                 server.Out.Info($"Disconnected {im.SenderConnection}");
                 server.SendToAllExcept(
                     server.ConvertToOutgoingMessage(
                         new ServerNotification { Message = $"Disconnected {im.SenderConnection}" }),
                     im.SenderConnection,
                     NetDeliveryMethod.ReliableOrdered,
                     0);
             });

            ConfigureConnectionApproval();
            ConfigureResponseDispatch();

        }

        private void ConfigureResponseDispatch()
        {
            server.RespondTo<ChatMessage>((msg, im, om) =>
            {
                server.Out.Info($"Received {msg.Message}");
                server.SendToAllExcept(om, im.SenderConnection, NetDeliveryMethod.ReliableOrdered, 0);
            });
        }

        private void ConfigureConnectionApproval()
        {
            
            server.IncomingMessageHandler.OnMessage(NetIncomingMessageType.ConnectionApproval, im =>
            {
                var msg = server.ReadAs<ConnectionApprovalMessage>(im);
                if (msg.Password == password)
                {
                    im.SenderConnection.Approve();
                    server.Out.Info($"Approved {msg.Sender} : {im.SenderConnection}");
                    server.SendToAllExcept(server.ConvertToOutgoingMessage(
                        new ServerNotification { Message = $"Say welcome to {msg.Sender}" }),
                        im.SenderConnection,
                        NetDeliveryMethod.ReliableOrdered,
                        0);
                }
                else
                {
                    im.SenderConnection.Deny();
                    server.Out.Info($"Denied {msg.Sender} : {im.SenderConnection}");
                }
            });
        }

        public void Start()
        {
            server.Host.Start();
            server.ProcessMessagesInBackground();
        }

        public void Stop()
        {
            server.Stop("bb");
        }
    }
}
