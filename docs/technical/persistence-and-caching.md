# Persistence and caching

## MongoDB access

[`ShortenerURLContext`](../../Src/Shortener.API/Infrastructure/ShortenerURLContext.cs) uses the MongoDB EF Core provider for link persistence and queries. [`MongoDependencyInjection`](../../Src/Shortener.API/Infrastructure/Extensions/MongoDependencyInjection.cs) also registers a native MongoDB client/database for atomic sequence updates.

Both registrations read `ConnectionStrings:ShortenerURLContext`. Include an explicit database name in that connection string so both access paths target the intended database. A singleton registration of the native client does not itself mean the EF provider uses that same client instance; EF is configured separately through its connection-string overload.

## Documents

| `UrlTags` field | CLR type | Purpose |
| --- | --- | --- |
| `Id` | `ObjectId` | Document identity |
| `ShortenedCode` | `string` | Public lookup code |
| `DestinationURL` | `string` | Destination used in the redirect |
| `CreatedOn` | `DateTime` | Creation time in UTC |
| `ExpirationDate` | `DateTime` | Expiration instant in UTC |

The `Sequences` collection contains a separate `SequenceDocument`: string `Id`, mapped with `[BsonId]`, and signed 64-bit `Value`. The current sequence identifier is `short-url`.

## Indexes and startup

The EF model declares:

| Field | Index | Current query usage |
| --- | --- | --- |
| `ShortenedCode` | Unique | Redirect lookup |
| `DestinationURL` | Nonunique | No destination lookup currently implemented |
| `ExpirationDate` | Nonunique | No expiration cleanup query currently implemented |

`Program.cs` creates a scope and awaits `context.Database.EnsureCreatedAsync()` before serving requests. Startup therefore requires MongoDB availability. Treat index deployment and verification on an existing database as an operational responsibility; model declarations alone are not evidence that every deployed database has the intended index state.

The expiration index is an ordinary index, not a TTL index. Expired links remain stored. This allows resolution to return 410 rather than losing the distinction between expired and never-created links. If cleanup is added, define the resulting API behavior explicitly.

## Redirect query

[`RedirectService`](../../Src/Shortener.API/Services/RedirectService.cs) filters by exact `ShortenedCode`, uses `AsNoTracking()`, and projects only `DestinationURL` and `ExpirationDate` into `RedirectCacheItem`. It retrieves the first matching document. The unique index is intended to ensure there can be only one.

## Cache resolution

With `CacheSettings:UseCache=true`, the service calls `IFusionCache.GetOrSetAsync` using `r:{shortCode}`. Redis is registered with instance prefix `Shortener:`. The cache library controls its physical storage-key representation.

Resolution proceeds through local memory, distributed cache, and then the supplied database factory. Factory executions increment `shortener.cache.database_fallback`. FusionCache provides coordination for concurrent fills within an application instance; there is no custom distributed lock in this project.

| Setting | Meaning |
| --- | --- |
| `ExpirationInMinutes` | Configured overall/distributed cache lifetime |
| `LocalCacheExpirationInMinutes` | Configured local memory lifetime |
| `UseCache` | Whether redirect resolution uses the cache at all |

Registration maps `LocalCacheExpirationInMinutes` to FusionCache `Duration` and `ExpirationInMinutes` to `DistributedCacheDuration`. The System.Text.Json serializer is registered explicitly for L2 values. Fail-safe is disabled, and background distributed operations and timed-out factory completion are disabled so database factory work remains within its owning request scope. No backplane is configured.

FusionCache uses its own serialized cache entries; existing HybridCache Redis entries are not reused. A rolling deployment can temporarily produce cold cache misses while each cache implementation populates its own entries. Existing MongoDB documents and public codes are unchanged.

With `UseCache=false`, Redis is not registered by `AddCache`, no Redis connection string is required, and redirects query MongoDB directly. FusionCache remains registered because `RedirectService` receives it through constructor injection.

The factory may return null for an unknown code; there is no separate application-defined negative-cache lifetime. There is also no application-defined TTL adjustment based on a link's remaining lifetime.

## Expiration and consistency

After retrieving an item from either cache or MongoDB, the service obtains current UTC time and rejects `ExpirationDate <= now`. A cached item therefore cannot bypass the service's expiration check.

The current API exposes no update or delete operation. Accordingly, it has no cache invalidation workflow. Editing or deleting documents outside the application can leave cache entries stale until their cache lifetime ends. A future update/delete endpoint must define invalidation across both cache layers.

There is no explicit Redis-error fallback policy in `RedirectService`. The database factory is the cache-miss path; it should not be confused with a project-defined policy for every distributed-cache failure. Exercise outage behavior in an integration environment before making availability guarantees.
