# HTTP API and errors

Source: [creation endpoint](../../Src/Shortener.API/Endpoints/ShortenerEndpoints.cs), [redirect endpoint](../../Src/Shortener.API/Endpoints/RedirectEndpoints.cs), and [request validator](../../Src/Shortener.API/Validators/ShortenUrlRequestValidator.cs).

## Create a link

`POST /api/v1/shortener` with `Content-Type: application/json`:

```json
{
  "longUrl": "https://example.com/articles/system-design",
  "expirationDate": "2030-01-01T00:00:00Z"
}
```

The expiration must be in the future at validation time. Replace the example date when necessary.

| Field | Rules |
| --- | --- |
| `longUrl` | Required; at most 4,096 characters; absolute HTTP/HTTPS URI; nonempty host; no control characters |
| `expirationDate` | `DateTimeOffset`; strictly greater than `TimeProvider.GetUtcNow()` |

Validation checks URL syntax. It does not fetch the destination, verify reachability, inspect content, or apply a destination allowlist. There is no configured maximum expiration horizon. Offset-bearing timestamps are converted to UTC before persistence.

On success, `Results.Ok(shortUrl)` returns status **200** with the URL as the response value. A JSON representation is a string, not an object:

```json
"https://short.example/0aK91PxQz"
```

There is no creation `Location` header or idempotency-key implementation. Repeating a request can create another link, including when a client retries after losing the response to a successful write.

## Resolve a link

`GET /0aK91PxQz`

The code must contain exactly nine characters from `0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz`. Codes are case-sensitive. The successful response is **302 Found**, with the destination in the `Location` header.

The `/{shortCode}` route accepts a single path segment, then validates it. Invalid lengths and characters return 400 when this endpoint handles the request. More specific application routes, such as the metrics endpoint, retain their own routing behavior. An unmatched multisegment path is not a short-code request.

| Outcome | HTTP status | Application code |
| --- | --- | --- |
| Invalid code | 400 | `SHORTENER.INVALID_SHORT_CODE` |
| Missing document | 404 | `SHORTENER.URL_NOT_FOUND` |
| Expiration at or before current UTC time | 410 | `SHORTENER.URL_EXPIRED` |
| Active link | 302 | None |

## Expected failures

`RedirectService` returns `Result<string>` for missing and expired links. The endpoint delegates conversion to `ErrorTranslator`. `Result<T>` contains `IsSuccess`, `Value`, and `ErrorCode`; callers must check success before consuming the value.

`ErrorCatalog` centralizes titles, messages, and HTTP status codes. An unrecognized code falls back to `COMMON.UNEXPECTED_ERROR`.

Example error body:

```json
{
  "type": "urn:shortener:error:SHORTENER.URL_EXPIRED",
  "title": "Short URL expired",
  "status": 410,
  "detail": "The requested short URL has expired.",
  "code": "SHORTENER.URL_EXPIRED",
  "traceId": "example-request-id"
}
```

Creation validation uses a typed validation result, preserving catalog metadata and adding an `errors` dictionary keyed by C# property names, such as `LongUrl` and `ExpirationDate`. The stable top-level code is `COMMON.VALIDATION_FAILED`, with status 400. Individual validator messages are currently defined in the validator itself.

Malformed JSON and request-binding failures occur before the explicit FluentValidation call. Do not assume every framework-generated 400 response carries the application's validation code.

## Unexpected failures

[`GlobalExceptionHandler`](../../Src/Shortener.API/Exceptions/GlobalExceptionHandler.cs) classifies exceptions through `ExceptionTranslator`, logs the exception with its error code, and writes Problem Details through `IProblemDetailsService`, with a JSON fallback.

| Exception classification | Code | Status |
| --- | --- | --- |
| `MongoExecutionTimeoutException` or `TimeoutException` | `INFRASTRUCTURE.DATABASE_TIMEOUT` | 503 |
| `MongoConnectionException` or other `MongoException` | `INFRASTRUCTURE.DATABASE_UNAVAILABLE` | 503 |
| Any other exception | `COMMON.UNEXPECTED_ERROR` | 500 |

Classification operates on the exception supplied to the handler; it does not recursively inspect inner exceptions. `SHORTENER.DUPLICATE_SHORT_CODE` is present in the catalog with status 409, but there is currently no dedicated duplicate-write translation/retry path. It is not guaranteed to be returned for a MongoDB uniqueness violation.

The handler does not include stack traces or exception messages in its public body. Request cancellation is rethrown by the redirect service after its metric result is set to `canceled`; final transport behavior depends on the HTTP pipeline and whether the client remains connected.
