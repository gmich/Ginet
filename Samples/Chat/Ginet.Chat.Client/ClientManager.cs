﻿using Ginet.Chat.Packages;
using Ginet.NetPackages;
using Lidgren.Network;
using System;
using System.Reflection;

namespace Ginet.Chat.Client
{
    internal class ClientManager
    {
        public readonly NetworkClient client;
        public IPackageSender DefaultPackageSender { get; }
        public IncomingMessageHandler MessageHandler => client.IncomingMessageHandler;

        public ClientManager(Action<GinetConfig> configuration)
        {
            client = new NetworkClient("Chat",
            configuration,
            container =>
                container.RegisterPackages(Assembly.Load("Ginet.Chat.Packages")));
            client.ProcessMessagesInBackground();

            DefaultPackageSender = client.LiftSender((msg, peer) =>
                peer.SendMessage(msg, NetDeliveryMethod.ReliableOrdered));
        }

        public void ConnectClient(ConnectionApprovalMessage message)
        {
            client.Connect("localhost", 1111, message);
        }

        public void Disconnect()
        {
            client.Stop("bb");
        }
    }

}

