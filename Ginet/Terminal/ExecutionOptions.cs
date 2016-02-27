namespace Ginet.Terminal
{
    public class ExecutionOptions
    {
        public bool OnlyWhiteListed { get; private set; }
        public ICommmandValidator Validator { get; private set; }

        public static ExecutionOptions WhiteListed => new ExecutionOptions { OnlyWhiteListed = true };

        public static ExecutionOptions Everyone => new ExecutionOptions();

        public ExecutionOptions WithValidator(ICommmandValidator validator)
        {
            Validator = validator;
            return this;
        }
    }



}
