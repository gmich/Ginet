using Ginet.Chat.Packages;
using Ginet.NetPackages;
using Lidgren.Network;
using System;

namespace Ginet.Chat.Client
{
    internal class Chatter
    {
        private readonly IPackageSender networkSender;
        private readonly string userName;

        public Chatter(IPackageSender sender, IncomingMessageHandler messageHandler)
        {
            Console.WriteLine("Username: ");
            userName = Console.ReadLine();
            networkSender = sender;
            ConfigureConnectionChange(messageHandler);
            ConfigurePackageHandling(messageHandler);
        }

        public ConnectionApprovalMessage ConnectionApprovalMessage
        => new ConnectionApprovalMessage
        {
            Sender = userName,
            Password = "1234"
        };

        private void ConfigureConnectionChange(IncomingMessageHandler messageHandler)
        {
            messageHandler
           .OnConnectionChange(NetConnectionStatus.Connected, im =>
                Console.WriteLine($"Connected to {im.SenderEndPoint}"));

        }
        private void ConfigurePackageHandling(IncomingMessageHandler messageHandler)
        {
            messageHandler
            .OnPackage<ChatMessage>((msg, sender) =>
                Console.WriteLine($"[{msg.Sender}]: {msg.Message}"));

            messageHandler
            .OnPackage<ServerNotification>((msg, sender) =>
                Console.WriteLine($"[Server]: {msg.Message }"));
        }

        public void SendChatMessage(string chatMsg)
        {
            networkSender.Send(new ChatMessage
            {
                Sender = userName,
                Message = chatMsg
            });
        }
               
    }
}
