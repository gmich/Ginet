using System.Diagnostics.Contracts;

namespace Ginet.Terminal
{
    public class CommandBuilder
    {
        private readonly CommandTableEntry entry;
        internal const string ArgumentPrefix = "-";

        internal CommandBuilder(string briefDescription, CommandDelegate callback, ExecutionOptions options)
        {
            Contract.Requires(!string.IsNullOrEmpty(briefDescription));
            Contract.Requires(callback != null);

            entry = new CommandTableEntry
            {
                BriefDescription = briefDescription,
                Callback = callback,
                Options = options
            };
        }

        public CommandBuilder WithArgument(string argument, string briefDescription, CommandDelegate callback, ExecutionOptions options)
        {
            Contract.Requires(!string.IsNullOrEmpty(argument));
            Contract.Requires(!string.IsNullOrEmpty(briefDescription));
            Contract.Requires(callback != null);
            entry.CommandArguments.Add(ArgumentPrefix + argument, new CommandTableEntry
            {
                BriefDescription = briefDescription,
                Callback = callback,
                Options = options
            });
            return this;
        }

        internal CommandTableEntry Build => entry;
    }
}
