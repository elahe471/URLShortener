namespace Shortener.API.Exceptions
{
    public static class ExceptionTranslator
    {
        public static string Translate(Exception exception)
        {
            // Handle specific MongoDB exceptions before the general MongoException.

            return exception switch
            {
                MongoExecutionTimeoutException =>
          ErrorCodes.DatabaseTimeout,

                MongoConnectionException =>
                    ErrorCodes.DatabaseUnavailable,

                MongoException =>
                    ErrorCodes.DatabaseUnavailable,

                TimeoutException =>
                    ErrorCodes.DatabaseTimeout,

                _ =>
                    ErrorCodes.Unexpected
            };
        }
    }
}
