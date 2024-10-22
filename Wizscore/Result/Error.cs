namespace Wizscore.Result
{
    public class Error
    {
        private Error(string message, string code)
        {
            Message = message;
            Code = code;
        }

        private Error(Exception exception, string code)
        {
            Exception = exception;
            Message = exception.Message;
            Code = code;
        }

        public string Message { get; }

        public string Code { get; }

        public Exception Exception { get; }


        public static readonly Error None = new Error(string.Empty, string.Empty);

        public static Error FromException(Exception ex, string code) => new Error(ex, code);

        public static Error FromError(string message, string code) => new Error(message, code);

    }
}
