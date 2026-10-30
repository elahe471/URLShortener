# Configuration and operations

## Prerequisites and local setup

Use the .NET 10 SDK, an accessible MongoDB deployment, and Redis when caching is enabled. For a local development setup, MongoDB and Redis can be supplied externally; this repository does not currently contain a Dockerfile or Compose deployment.

From the repository root:

```powershell
dotnet restore URLShortener.slnx
dotnet user-secrets set "ConnectionStrings:ShortenerURLContext" "mongodb://localhost:27017/shortener" --project Src/Shortener.API
dotnet user-secrets set "ShortenerSettings:BaseUrl" "http://localhost:5004" --project Src/Shortener.API
dotnet user-secrets set "CacheSettings:UseCache" "false" --project Src/Shortener.API
```

Generate a key once, store it, and preserve it for this database's lifetime:

```powershell
$shortenerKey = [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
dotnet user-secrets set "ShortenerSettings:SecretKey" $shortenerKey --project Src/Shortener.API
```

Run the application:

```powershell
dotnet run --project Src/Shortener.API --launch-profile http
```

The HTTP profile binds port 5004 on all interfaces. The HTTPS profile additionally exposes `https://localhost:7261`. `BaseUrl` controls generated links independently of the listening address; set it to the externally reachable address.

To enable Redis:

```powershell
dotnet user-secrets set "ConnectionStrings:Redis" "localhost:6379" --project Src/Shortener.API
dotnet user-secrets set "CacheSettings:UseCache" "true" --project Src/Shortener.API
```

User Secrets are a development mechanism. Supply production secrets through the deployment environment or a secret store.

## Settings reference

| Configuration key | Purpose / validation |
| --- | --- |
| `ConnectionStrings:ShortenerURLContext` | MongoDB connection string, including the intended database |
| `ConnectionStrings:Redis` | Required and nonblank when `UseCache=true` |
| `ShortenerSettings:BaseUrl` | Absolute HTTP/HTTPS URL with host; no credentials, query, or fragment |
| `ShortenerSettings:SecretKey` | Base64 encoding of at least 32 bytes |
| `CacheSettings:UseCache` | Enables cached redirect resolution |
| `CacheSettings:ExpirationInMinutes` | Cache lifetime; checked-in environment settings use 30 |
| `CacheSettings:LocalCacheExpirationInMinutes` | Memory lifetime; checked-in environment settings use 5 |

The correct section name is **`CacheSettings`**. The shortener options use `ValidateOnStart`. Cache durations are passed to FusionCache; the project has no dedicated options-validation rules for their ranges.

Environment variable equivalents replace `:` with `__`, for example `ShortenerSettings__SecretKey` and `ConnectionStrings__ShortenerURLContext`.

## Startup sequence

`Program.cs` registers OpenAPI, application services, validators, MongoDB, caching, Problem Details, exception handling, and OpenTelemetry metrics. After building the host, it initializes the database, maps OpenAPI/Scalar, installs exception handling, maps business endpoints, enables Prometheus scraping, and runs the host.

Database initialization happens before the HTTP exception middleware can handle requests. Initialization failures are startup failures rather than application Problem Details responses.

## Metrics

Source: [`ShortDiagnostic`](../../Src/Shortener.API/Observability/ShortDiagnostic.cs). Meter name: `Shortener.API`. Prometheus scraping is exposed through the configured ASP.NET Core exporter at `/metrics`.

| Instrument | Type | Interpretation |
| --- | --- | --- |
| `shortener.links.created` | Counter | Successfully persisted short links |
| `shortener.redirects` | Counter | Resolutions grouped by `result` |
| `shortener.redirect.duration` | Histogram, seconds | Service resolution time grouped by `result` |
| `shortener.cache.database_fallback` | Counter | Executions of the cache's database factory |

Redirect results are `success`, `not_found`, `expired`, `error`, and `canceled`. Invalid-code requests rejected by the endpoint do not enter `RedirectService` and therefore do not increment its resolution instruments. The duration excludes endpoint processing and response transmission. Direct database reads with caching disabled do not increment the cache-fallback counter.

ASP.NET Core instrumentation provides HTTP metrics in addition to these application instruments. Exported Prometheus names can differ from source instrument names due to exporter naming conventions. Codes and destination URLs are not application metric labels.

The application logs cache misses at debug level and exceptions through the centralized handler. It does not currently configure a tracing pipeline, log collector, or tracing backend.

## Deployment boundary

The current application maps OpenAPI/Scalar without an environment guard, has HTTPS redirection commented out, and does not configure authentication, rate limiting, or readiness/liveness endpoints. A deployment should explicitly decide which responsibilities belong to the service and which are enforced by its gateway or reverse proxy. These protections must not be assumed from the existing code.

All writers sharing a URL collection need compatible key and sequence state. Back up the `Sequences` collection together with `UrlTags`; restoring URL documents while resetting the counter can reissue inputs and cause uniqueness failures.
