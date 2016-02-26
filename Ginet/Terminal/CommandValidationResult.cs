namespace Ginet.Terminal
{
    public class CommandValidationResult
    {
        public bool Success { get; }
        public string Message { get; }
        public CommandValidationResult(bool success, string message)
        {
            Success = Success;
            Message = message;
        }

    }

}
