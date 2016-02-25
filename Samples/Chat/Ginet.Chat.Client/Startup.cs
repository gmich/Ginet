using System;

namespace Ginet.Chat.Client
{
    internal class Startup
    {
        static void Main(string[] args)
        {
            var clientManager = new ClientManager(cfg=> { });
            var chatter = new Chatter(clientManager.DefaultPackageSender,clientManager.MessageHandler);
            clientManager.ConnectClient(chatter.ConnectionApprovalMessage);

            string input;
            while((input = Console.ReadLine()) != "quit")
            {
                chatter.SendChatMessage(input);
            }

            clientManager.Disconnect();
        }
    }
}
