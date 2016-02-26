using System.Collections.Generic;

namespace Ginet.Terminal
{

    public interface ICommmandValidator
    {
        CommandValidationResult Validate(IEnumerable<string> arguments);
    }
}
