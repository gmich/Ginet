﻿using Ginet.Logging;
using Lidgren.Network;

namespace Ginet
{
    public class GinetConfig
    {
        public IAppender Output { get; set; }
        public bool EnableAllIncomingMessages { get; set; }
        public NetPeerConfiguration NetConfig { get; internal set; }
        public int DefaultChannel { get; set; }
        public NetDeliveryMethod DeliveryMethod { get; set; }
    }
}