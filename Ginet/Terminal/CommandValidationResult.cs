namespace Ginet.Terminal
{
    public class CommandValidationResult
    {
        public bool Success { get; }
        public string Message { get; }
        public CommandValidationResult(bool success, string message)
        {
            Success = success;
            Message = message;
        }

        public static CommandValidationResult Ok =>
            new CommandValidationResult(true, string.Empty);

        public static CommandValidationResult Error(string msg) =>
            new CommandValidationResult(false, msg);

    }

}
