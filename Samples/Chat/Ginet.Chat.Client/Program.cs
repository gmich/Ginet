using Ginet.Chat.Packages;
using Lidgren.Network;
using System;

namespace Ginet.Chat.Client
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var chatClient = new ChatClient();
            string input;

            while((input = Console.ReadLine()) != "quit")
            {
                chatClient.Sender.Send(new ChatMessage { Message = input });
            }

            chatClient.Client.Stop("bb");
        }
    }
}
