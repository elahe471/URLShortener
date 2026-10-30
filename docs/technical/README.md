# Technical documentation

This documentation explains the implementation currently in this repository. It is intended for developers reviewing the code or extending the Shortener service.

## Reading order

1. [Architecture and code map](architecture.md): service boundary, composition, dependencies, and request flows.
2. [HTTP API and errors](api-and-errors.md): request contracts, validation, redirects, and failure handling.
3. [Short-code generation](short-code-generation.md): atomic sequences, Feistel permutation, uniqueness assumptions, and capacity.
4. [Persistence and caching](persistence-and-caching.md): MongoDB documents, indexes, cache behavior, and consistency.
5. [Configuration and operations](configuration-and-operations.md): local setup, settings, startup, deployment considerations, and telemetry.
6. [Testing and extension guide](testing-and-extension.md): current coverage, verification commands, and change impact.

## Implementation scope

The service creates expiring short links and resolves their destinations. It exposes a REST creation endpoint and a public redirect endpoint. MongoDB owns persistent state; Redis is an optional distributed cache. Components inside the process communicate through direct method calls.

The repository contains one ASP.NET Core application and one test project. References to a larger microservice system describe how this service can fit into that system; there are no implemented broker integrations or interservice RPC clients.

Examples use illustrative codes and hostnames. Generated codes depend on the configured secret and sequence state.
