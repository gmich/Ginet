using Ginet.Packages;

namespace Ginet.Chat.Packages
{
    [GinetPackage]
    public class ConnectionApprovalMessage
    {
        public string Sender { get; set; }
        public string Password { get; set; }

    }
}
