using System.Collections.Generic;

namespace Ginet.Terminal
{
    public class ExecutionResult
    {
        public enum Status
        {
            Ok,
            Faulted,
            CommandNotFound,
            Unauthorized
        }

        public Status ExecutionStatus { get; }
        public string ErrorMessage { get; }
        public string Result { get; }
        public bool Successful => ExecutionStatus == Status.Ok;

        private ExecutionResult(Status status, string errorMessage, string result)
        {
            ExecutionStatus = status;
            ErrorMessage = errorMessage;
            Result = result;
        }

        public static ExecutionResult Ok(string result)
        {
            return new ExecutionResult(Status.Ok, string.Empty, result);
        }

        public static ExecutionResult Ok()
        {
            return new ExecutionResult(Status.Ok, string.Empty, null);
        }

        public static ExecutionResult Faulted(Status status, string reason)
        {
            return new ExecutionResult(status, reason, null);
        }

    }

}