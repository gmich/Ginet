using Ginet.Chat.Packages;
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
            server = new NetworkServer("Chat", 
            container=>
                container.RegisterPackages(Assembly.Load("Ginet.Chat.Packages")),
            cfg =>
            {
                cfg.Port = 1234;
                cfg.ConnectionTimeout = 5.0f;
            });
            server.IncomingMessageHandler.LogTraffic();

            server.IncomingMessageHandler.OnConnectionChange(NetConnectionStatus.Disconnected, im =>             
                 server
                 .SendToAllExcept(new ServerNotification
                 {
                     Message = $"Disconnected {im.SenderEndPoint}"
                 },
                 im.SenderConnection)
             );

            ConfigureConnectionApproval();
            ConfigureResponses();
        }

        private void ConfigureResponses()
        {
            server.BroadcastExceptSender<ChatMessage>((sender,msg) =>            
                server.Out.Info($"Received {msg.Message} from {sender}")
            );
        }

        private void ConfigureConnectionApproval()
        {            
            server.IncomingMessageHandler.OnMessage(NetIncomingMessageType.ConnectionApproval, im =>
            {
                var msg = server.ReadAs<ConnectionApprovalMessage>(im);
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
            });
        }

        public void Start()
        {
            server.Start(NetDeliveryMethod.ReliableOrdered, 0);
            server.ProcessMessagesInBackground();            
        }

        public void Stop()
        {
            server.Stop("bye");
        }
    }
}
