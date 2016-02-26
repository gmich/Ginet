namespace Ginet.Terminal
{
    public class ExecutionOptions
    {
        public static ExecutionOptions None => new ExecutionOptions();
        public bool OnlyWhiteListed { get; set; }
        public ICommmandValidator Validator { get; set; }
    }

}
