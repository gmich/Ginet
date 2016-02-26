
namespace Ginet.Terminal
{

    internal class CommandTableEntry
    {
        public string Description { get; set; }
        public CommandDelegate Callback { get; set; }
        public ExecutionOptions Options { get; set; }
    }
}
