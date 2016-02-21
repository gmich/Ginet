using Ginet.Chat.Packages;
using Ginet.Client;
using Ginet.Packages;
using Lidgren.Network;
using System;
using System.Reflection;

namespace Ginet.Chat.Client
{
    internal class ChatClient
    {
        public NetworkClient Client { get; }

        public IPackageSender Sender { get; }

        public ChatClient()
        {
            Client = new NetworkClient("Chat", cfg =>
            { });

#if DEBUG
            //Client.IncomingMessageHandler.LogTraffic();
#endif
            Client.PackageConfigurator.RegisterPackages(Assembly.Load("Ginet.Chat.Packages"));

            Client.Connect("localhost", 1234, new ConnectionApprovalMessage
            {
                Sender = "Me",
                Password = "1234"
            });
            Client.ProcessMessagesInBackground();

            Client
            .IncomingMessageHandler
            .OnPackage<ChatMessage>((msg, sender) => 
                Console.WriteLine(msg.Message));

            Client
            .IncomingMessageHandler
            .OnPackage<ServerNotification>((msg, sender) => 
                Console.WriteLine($"[Server]: {msg.Message }"));

            Sender = Client.LiftSender((msg, peer) =>
              peer.SendMessage(msg, NetDeliveryMethod.ReliableOrdered));
        }

    }
}
