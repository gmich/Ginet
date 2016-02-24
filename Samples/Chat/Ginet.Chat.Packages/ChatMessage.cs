using Ginet.NetPackages;

namespace Ginet.Chat.Packages
{
    [GinetPackage]
    public class ChatMessage
    {
        public string Sender { get; set; }
        public string Message { get; set; }
    }
}
