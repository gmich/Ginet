
using System.Collections.Generic;

namespace Ginet.Terminal
{

    internal class CommandTableEntry
    {
        public string BriefDescription { get; set; }
        public CommandDelegate Callback { get; set; }
        public Dictionary<string, CommandTableEntry> CommandArguments { get; set; }
        public ExecutionOptions Options { get; set; }
    }
}
