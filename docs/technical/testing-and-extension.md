# Testing and extension guide

## Run verification

From the repository root:

```powershell
dotnet test URLShortener.slnx
```

After dependencies have been restored, use `--no-restore` when appropriate. `dotnet test` builds the referenced API and test projects before executing the tests.

## Current automated coverage

| Test file | Behavior covered |
| --- | --- |
| [ShortCodeGeneratorTests.cs](../../Tests/Shortener.UnitTests/Services/ShortCodeGeneratorTests.cs) | Nine-character Base62 output; determinism; distinct sample inputs; nonpositive inputs; capacity boundary; 100,000 unique inputs |
| [ShortenUrlRequestValidatorTests.cs](../../Tests/Shortener.UnitTests/Validators/ShortenUrlRequestValidatorTests.cs) | Future, past, and exact-current expiration using `FakeTimeProvider` |
| [RegressionTests.cs](../../Tests/Shortener.UnitTests/RegressionTests.cs) | Timeout classification/catalog mapping; typed validation response metadata; invalid code rejection before resolution; disabled cache registration; invalid BaseUrl options |

| [RedirectCacheTests.cs](../../Tests/Shortener.UnitTests/Services/RedirectCacheTests.cs) | FusionCache L1/L2 durations; serialized L2 sharing across instances using an in-memory distributed test double; destination caching and expiration after a cache hit |

There are currently 27 executable test cases, including theory rows. These tests do not establish real MongoDB concurrency guarantees, actual deployed index state, Redis outage behavior, or complete HTTP binding/routing behavior. Direct endpoint invocation bypasses the HTTP host and middleware.

## Useful integration scenarios

Use an isolated database and cache namespace so tests cannot alter production data.

1. Submit a valid creation request, resolve the returned path, and verify the 302 destination.
2. Resolve a link through a cache miss and hit, advance time to its expiration, and verify 410 in both cases.
3. Allocate sequences concurrently against MongoDB, including initialization, and verify distinct returned values.
4. Inspect the actual MongoDB indexes and verify duplicate codes cannot be persisted.
5. Exercise unavailable MongoDB and Redis separately and observe status codes, latency, logs, and metrics.
6. Test invalid JSON and invalid-code routes through an HTTP host rather than direct method calls.

These are recommended additional scenarios, not tests already implemented in this repository.

## Change impact map

| Intended change | Files / concerns to inspect |
| --- | --- |
| Add a creation input | Request contract, FluentValidation rules, endpoint call, service contract, persistence model, API documentation |
| Change code format | Generator capacity/encoding, redirect validation, existing stored codes, tests, public compatibility |
| Add a known error | Search `ErrorCodes` first; reuse an equivalent code; update `ErrorCatalog` and appropriate translation path |
| Add update/delete | Persistence behavior, cache invalidation, consistency across replicas, expected error results |
| Change expiration semantics | Validator, UTC persistence, post-cache check, exact-boundary tests, cleanup policy |
| Change allocation strategy | Sequence generator, cross-replica uniqueness, recovery behavior, existing namespace |
| Add analytics | Decide what is counted: resolution, response, or external click; define delivery and failure semantics |

## Contributor constraints

Keep endpoints focused on HTTP orchestration and services focused on the workflow. Reuse the existing result/error infrastructure. Propagate cancellation into I/O operations. Avoid introducing a broker, mediator, new architectural layer, or another mapping library without a concrete need.

Do not change a deployed code format or secret casually: these affect compatibility or uniqueness assumptions. When adding behavior, test externally meaningful outcomes and update the corresponding technical document.
