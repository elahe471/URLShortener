# URL Shortener

A production-oriented URL Shortener API built with **.NET Minimal API**, **MongoDB**, and **HybridCache**.

The project is being developed as a practical system-design exercise, with focus on clean architecture, MongoDB concepts, secure short-code generation, centralized error handling, configuration management, caching, and unit testing.

## Current Features

- Create shortened URLs
- Redirect short URLs to their original destination
- URL validation with FluentValidation
- Expiration support
- Environment-specific base URLs
- MongoDB persistence
- Unique and query indexes
- Atomic sequence generation with MongoDB
- Non-sequential, hard-to-guess short codes
- Hybrid caching with in-memory cache and Redis
- Configurable cache enable/disable switch
- Centralized error handling with Problem Details
- Unit tests for short-code generation

## Tech Stack

- .NET 10
- ASP.NET Core Minimal APIs
- MongoDB
- MongoDB Entity Framework Core Provider
- MongoDB .NET Driver
- Microsoft.Extensions.Caching.Hybrid
- StackExchange.Redis / Distributed Cache
- FluentValidation
- xUnit
- FluentAssertions
- NSubstitute
- Scalar / OpenAPI

## API Structure

### Create Short URL

```http
POST /api/v1/shortener
```

Example request:

```json
{
  "longUrl": "https://example.com/some/very/long/url"
}
```

Example response:

```text
https://localhost:7261/0aK91PxQz
```

### Redirect

```http
GET /{shortCode}
```

Example:

```http
GET /0aK91PxQz
```

If the short code is valid and not expired, the API redirects the client to the stored destination URL.

## MongoDB Model

The main URL document contains data similar to:

```javascript
{
  ShortenedCode: "0aK91PxQz",
  DestinationURL: "https://example.com/...",
  CreatedOn: ISODate("2026-09-08T10:00:00Z"),
  ExpirationDate: ISODate("2026-10-08T10:00:00Z")
}
```

A separate sequence document is used to atomically generate unique numeric IDs.

```javascript
{
  _id: "short-url",
  Value: 12345
}
```

## MongoDB Access Strategy

The project intentionally uses both the MongoDB EF Core provider and the native MongoDB Driver.

```text
MongoDB
│
├── EF Core Provider
│   └── UrlTags
│       ├── queries
│       └── persistence
│
└── Native MongoDB Driver
    └── Sequences
        └── atomic findOneAndUpdate + $inc
```

EF Core is used for regular persistence operations on URL documents.

The native MongoDB Driver is used for sequence generation because MongoDB-native atomic operations such as `$inc`, `findOneAndUpdate`, and `upsert` are a better fit for this requirement.

`MongoClient` is registered as a Singleton because it is thread-safe and manages its own connection pool internally.

`IMongoDatabase` is also registered as a Singleton because it is lightweight, thread-safe, and reuses the shared Mongo client.

## Short Code Generation

The short-code generator does not query MongoDB to check whether a randomly generated value already exists.

Instead, the generation pipeline is:

```text
MongoDB Atomic Counter
        ↓
Unique Sequence
        ↓
52-bit Feistel Permutation
        ↓
HMAC-SHA256 Round Function
        ↓
Base62 Encoding
        ↓
9-character Short Code
```

### Why this design?

#### Atomic Counter

MongoDB `$inc` provides a unique sequence value atomically.

```text
Request A → 1001
Request B → 1002
Request C → 1003
```

This removes the need for an additional database lookup before every insert.

#### Feistel Permutation

Sequential IDs are predictable.

```text
1001
1002
1003
```

The Feistel network transforms them into a non-sequential permutation while preserving uniqueness.

The round function uses **HMAC-SHA256** with a secret key.

The secret key is not intended to encrypt user data. Its purpose here is to make the sequence-to-short-code mapping difficult to predict externally.

#### Base62

The final numeric value is encoded with:

```text
0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz
```

This makes the code compact and URL-safe.

The current short-code length is **9 characters** and the generator operates within a 52-bit space:

```text
2^52 - 1
```

## Database Indexes

The following indexes are currently used:

```text
ShortenedCode
→ Unique Index

DestinationURL
→ Query Index

ExpirationDate
→ Query Index
```

The unique index on `ShortenedCode` remains as the final database-level safety guarantee even though the generation algorithm is designed to preserve uniqueness.

## Hybrid Cache

The redirect flow uses ASP.NET Core `HybridCache`.

HybridCache combines two cache layers:

```text
L1 → In-memory cache
L2 → Redis distributed cache
```

The lookup flow is:

```text
Request
  ↓
L1 Memory Cache
  ↓ miss
L2 Redis
  ↓ miss
MongoDB
  ↓
Populate Redis + Memory
  ↓
Return
```

This design reduces MongoDB reads for frequently accessed short URLs while keeping redirect resolution fast.

HybridCache also provides stampede protection for concurrent requests targeting the same cache key within the same application instance. When multiple requests miss the cache at the same time, the underlying factory is coordinated so the database does not need to be queried repeatedly for the same key.

### Redirect Cache Key

Redirect entries use a short namespace prefix:

```text
r:{shortCode}
```

Example:

```text
r:0aK91PxQz
```

The prefix helps avoid collisions with other cache entry types if more caching scenarios are added later.

### Redirect Cache Item

Only the fields required for redirect resolution are stored in the cache:

```csharp
public sealed record RedirectCacheItem(
    string DestinationURL,
    DateTime ExpirationDate);
```

This allows the application to validate the URL expiration date even when the result is returned from memory or Redis.

### Cache Configuration

Caching can be enabled or disabled through `CacheSettings`.

Example configuration:

```json
{
  "Cache": {
    "UseCache": true,
    "ExpirationInMinutes": 30,
    "LocalCacheExpirationInMinutes": 5
  }
}
```

Current cache settings:

```text
UseCache
→ Enables or disables the cache path.

ExpirationInMinutes
→ Controls the distributed Redis cache lifetime.

LocalCacheExpirationInMinutes
→ Controls the in-process memory cache lifetime.
```

Typical flow:

```text
L1 Memory → 5 minutes
L2 Redis  → 30 minutes
```

If caching is disabled, the redirect service reads directly from MongoDB.

```text
UseCache = false
        ↓
MongoDB
```

### Expiration Safety

The URL's actual `ExpirationDate` is checked after the value is resolved, regardless of whether it came from:

```text
Memory
Redis
MongoDB
```

This ensures an expired URL is never redirected even if a cache entry still exists.

## Configuration

The project uses ASP.NET Core configuration and the Options pattern for application settings.

### Shortener Settings

```csharp
IOptions<ShortenerSettings>
```

Current shortener settings include:

```text
BaseUrl
ExpireDateScopeInDays
SecretKey
```

### Cache Settings

Cache behavior is configured independently:

```text
UseCache
ExpirationInMinutes
LocalCacheExpirationInMinutes
```

### Development

Non-sensitive settings can live in:

```text
appsettings.Development.json
```

Sensitive values are stored with User Secrets.

Examples:

```text
ShortenerSettings:SecretKey
ConnectionStrings:ShortenerURLContext
ConnectionStrings:Redis
```

### Production

Environment-specific non-sensitive settings live in:

```text
appsettings.Production.json
```

Sensitive values should be supplied through environment variables or a secret store.

Example environment variable format:

```text
ShortenerSettings__SecretKey
ConnectionStrings__ShortenerURLContext
ConnectionStrings__Redis
```

## Error Handling

The API uses centralized error handling based on modern ASP.NET Core primitives:

- `IExceptionHandler`
- `IProblemDetailsService`
- `ProblemDetails`
- stable application error codes
- a central error catalog
- an exception translator

The flow is:

```text
Exception
    ↓
ExceptionTranslator
    ↓
ErrorCode
    ↓
ErrorCatalog
    ↓
GlobalExceptionHandler
    ↓
ProblemDetails
```

Expected business outcomes are not modeled as exceptions.

Examples:

```text
URL not found
URL expired
Validation failure
```

These are represented using `Result<T>` and stable error codes.

Infrastructure failures such as MongoDB connectivity or persistence failures are handled by the global exception handler.

Example error response:

```json
{
  "type": "urn:shortener:error:SHORTENER.URL_EXPIRED",
  "title": "Short URL expired",
  "status": 410,
  "detail": "The requested short URL has expired.",
  "code": "SHORTENER.URL_EXPIRED",
  "traceId": "..."
}
```

## Redirect Resolution

The redirect query projects only the fields needed for redirect resolution:

```text
DestinationURL
ExpirationDate
```

This avoids loading the entire MongoDB document for every redirect request.

With caching enabled, redirect resolution follows this path:

```text
Short Code
    ↓
HybridCache
    ↓
Memory
    ↓ miss
Redis
    ↓ miss
MongoDB
    ↓
Expiration Check
    ↓
302 Redirect
```

Current redirect behavior:

```text
Invalid short code → 400
Short code not found → 404
Expired URL → 410
Valid URL → 302 Redirect
Database unavailable → 503
Unexpected error → 500
```

## Time Handling

The application uses `TimeProvider` instead of directly depending on `DateTime.UtcNow`.

This improves testability, especially for expiration-related behavior.

```csharp
TimeProvider.System
```

is registered in DI for normal application use.

## Unit Tests

A dedicated test project is used:

```text
Tests/
└── Shortener.UnitTests/
```

Current tests focus on `ShortCodeGenerator`.

They verify:

- output length is 9 characters
- output contains only Base62 characters
- the same sequence produces the same short code
- different sequences produce different short codes
- invalid sequences are rejected
- the maximum supported sequence succeeds
- values beyond the 52-bit range fail
- a large range of sequential inputs produces no duplicates

Example test naming convention:

```text
MethodName_Scenario_ExpectedResult
```

Example:

```csharp
Generate_WithInvalidSequence_ShouldThrow()
```

The tests follow the AAA pattern:

```text
Arrange
Act
Assert
```

## Testing Strategy

Pure application logic is covered by Unit Tests.

MongoDB atomic behavior should be covered by Integration Tests rather than mocked Unit Tests.

Planned MongoDB integration test:

```text
100 concurrent requests
        ↓
MongoDB atomic $inc
        ↓
100 different sequence values
```

A future integration-test project can use a real MongoDB container through Testcontainers.

### HybridCache Verification

The current HybridCache flow has been manually verified for the main cache paths:

```text
Cold Cache
→ Memory miss
→ Redis miss
→ MongoDB

Second Request
→ Memory hit

Application Restart
→ Memory cleared
→ Redis hit

UseCache = false
→ MongoDB directly
```

This confirms the intended L1/L2/fallback behavior of the redirect path.

## Current Project Direction

The project is intentionally being kept simple at the API level while exploring deeper system-design concepts where they provide real value.

Current focus areas include:

```text
URL validation
MongoDB
Atomic ID generation
Feistel permutation
Base62 encoding
Centralized error handling
Environment configuration
Hybrid caching
Unit testing
```

Future areas may include:

- integration tests with MongoDB
- integration tests for cache behavior
- redirect analytics
- click tracking
- rate limiting
- cache invalidation strategy
- cleanup of expired URLs
- distributed ID-generation strategies
- observability and metrics
