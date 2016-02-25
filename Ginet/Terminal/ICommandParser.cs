using System.Collections.Generic;

namespace Ginet.Terminal
{

    public interface ICommandParser
    {
        IEnumerable<CommandInfo> Parse(string text);
    }

}
