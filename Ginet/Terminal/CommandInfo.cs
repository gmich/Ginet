using System.Collections.Generic;

namespace Ginet.Terminal
{
    public class CommandInfo
    {

        public enum ContinuationOption
        {
            Flush,
            Append
        }

        public ContinuationOption Continuation { get; set; }
        public string Command { get; set; }
        public IList<string> Arguments { get; set; } = new List<string>();
        public IEnumerable<CommandInfo> Argumements { get; set; } = new List<CommandInfo>();
    }

}
