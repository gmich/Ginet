using Lidgren.Network;
using System.Collections.Generic;

namespace Ginet.Terminal
{
    public delegate ExecutionResult CommandDelegate(IEnumerable<string> arguments);

}