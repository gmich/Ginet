using System;
using System.Collections.Generic;
using System.Linq;

namespace Ginet.Terminal
{
    public class CommandValidator : ICommmandValidator
    {
        private readonly Func<IEnumerable<string>, CommandValidationResult> validate;
        public CommandValidator(Func<IEnumerable<string>,CommandValidationResult> validate)
        {
            this.validate = validate;
        }
        public CommandValidationResult Validate(IEnumerable<string> arguments)
        {
            return validate(arguments);
        }

        public static CommandValidator ZeroArguments =>
            new CommandValidator(args =>
            args.Any() ?
            CommandValidationResult.Error("Command requires zero arguments") :
            CommandValidationResult.Ok);


    }
}
