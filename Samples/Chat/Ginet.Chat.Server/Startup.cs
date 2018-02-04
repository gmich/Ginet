using System;
using Topshelf;

namespace Ginet.Chat.Server
{
    internal class Startup
    {
        static void Main(string[] args)
        {
            HostFactory.Run(x =>
            {
                x.Service<ChatServer>(s =>
                {
                    s.ConstructUsing(name => new ChatServer());
                    s.WhenStarted(tc => tc.Start());
                    s.WhenStopped(tc => tc.Stop());
                });
                x.RunAsLocalSystem();
                x.SetDescription("GinetChat Windows Service");
                x.SetDisplayName("GinetChat Windows Service");
                x.SetServiceName("GinetChat");
            });

            Console.ReadLine();
        }
    }
}
