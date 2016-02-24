using Ginet.Chat.Packages;
using Ginet.NetPackages;
using Lidgren.Network;
using System;
using System.Reflection;

namespace Ginet.Chat.Client
{
    internal class ChatClient
    {
        public NetworkClient Client { get; }

        public IPackageSender Sender { get; }

        public string UserName { get; }
        public ChatClient(string userName)
        {
            UserName = userName;
            Client = new NetworkClient("Chat", cfg =>
            { });
            //Client.IncomingMessageHandler.LogTraffic();
            Client.PackageConfigurator.RegisterPackages(Assembly.Load("Ginet.Chat.Packages"));
            Client.Start(NetDeliveryMethod.UnreliableSequenced, 0);

            Client.Connect("localhost", 1234, new ConnectionApprovalMessage
            {
                Sender = userName,
                Password = "1234"
            });
            Client.ProcessMessagesInBackground();

            Client
            .IncomingMessageHandler
            .OnConnectionChange(NetConnectionStatus.Connected, im =>
                 Console.WriteLine($"Connected to {im.SenderEndPoint}"));

            Client
            .IncomingMessageHandler
            .OnPackage<ChatMessage>((msg, sender) =>
                Console.WriteLine($"[{msg.Sender}]: {msg.Message}"));

            Client
            .IncomingMessageHandler
            .OnPackage<ServerNotification>((msg, sender) =>
                Console.WriteLine($"[Server]: {msg.Message }"));

            Sender = Client.LiftSender((msg, peer) =>
              peer.SendMessage(msg, NetDeliveryMethod.ReliableOrdered));
        }

    }
}
