namespace invoice_v1.src.Application.Exceptions
{
    public class RateLimitExceededException : Exception
    {
        public RateLimitExceededException(string message) : base(message) { }
    }
}
