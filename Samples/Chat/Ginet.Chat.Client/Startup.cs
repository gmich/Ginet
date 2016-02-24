using Ginet.Chat.Packages;
using Lidgren.Network;
using System;

namespace Ginet.Chat.Client
{
    internal class Startup
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Username: ");
            var chatClient = new ChatClient(Console.ReadLine());
            string input;

            while((input = Console.ReadLine()) != "quit")
            {
                chatClient.Sender.Send(new ChatMessage
                {
                    Sender = chatClient.UserName,
                    Message = input
                });
            }
            chatClient.Client.Stop("bb");
        }
    }
}
