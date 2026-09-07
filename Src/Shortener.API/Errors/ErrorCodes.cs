namespace Shortener.API.Errors
{
    public static class ErrorCodes
    {
        public const string InvalidShortCode =
            "SHORTENER.INVALID_SHORT_CODE";

        public const string UrlNotFound =
            "SHORTENER.URL_NOT_FOUND";

        public const string UrlExpired =
            "SHORTENER.URL_EXPIRED";

        public const string DuplicateShortCode =
            "SHORTENER.DUPLICATE_SHORT_CODE";

        public const string DatabaseUnavailable =
            "INFRASTRUCTURE.DATABASE_UNAVAILABLE";

        public const string Unexpected =
            "COMMON.UNEXPECTED_ERROR";
 

        public const string DatabaseTimeout =
            "INFRASTRUCTURE.DATABASE_TIMEOUT";

        public const string ValidationFailed =
    "COMMON.VALIDATION_FAILED";

    }
}
