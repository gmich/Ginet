using Ginet.Logging;
using Lidgren.Network;
using System;

namespace Ginet
{
    public class GinetConfig
    {
        public IAppender Output { get; set; }
        public bool EnableAllIncomingMessages { get; set; } = true;
        public NetPeerConfiguration NetConfig { get; internal set; }
        public int DefaultChannel { get; set; }
        public NetDeliveryMethod DeliveryMethod { get; set; }
        public Action<string> TerminalOutput { get; set; } = Console.WriteLine;
    }
}
