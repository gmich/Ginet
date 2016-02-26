using Ginet.NetPackages;

namespace Ginet.Terminal
{
    [GinetPackage]
    public class Command
    {
        public string Sender { get; set; }

        public string CommandText { get; set; }
    }
}
