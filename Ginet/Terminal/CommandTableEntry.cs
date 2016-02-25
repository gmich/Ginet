
namespace Ginet.Terminal
{

    internal class CommandTableEntry
    {
        public bool OnlyWhiteListed { get; set; }
        public string Description { get; set; }
        public CommandDelegate Callback { get; set; }
    }
}
