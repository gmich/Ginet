﻿using Lidgren.Network;
using System;

namespace Ginet.Chat.Client
{
    internal class Startup
    {
        static void Main(string[] args)
        {
            var clientManager = new ClientManager(cfg =>
            {
                cfg.DeliveryMethod = NetDeliveryMethod.ReliableOrdered;
            });
            var chatter = new Chatter(clientManager.DefaultPackageSender, clientManager.MessageHandler);
            clientManager.ConnectClient(chatter.ConnectionApprovalMessage);

            string input;
            while ((input = Console.ReadLine()) != "quit")
            {
                chatter.SendChatMessage(input);
            }

            clientManager.Disconnect();
        }
    }
}
