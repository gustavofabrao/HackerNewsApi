# Santander - Hacker News Best Stories API

ASP.NET Core (.NET 8) REST API that returns the first **n best stories** from Hacker News (sorted by score desc).

## Features
- GET `/api/beststories?n=10`
- L1 cache: in-memory
- L2 cache: Redis (distributed)
- Resilience with Polly: retry (exp backoff), timeout, circuit breaker
- Concurrency limiting for item fetches
- Middleware: Correlation ID (`X-Correlation-Id`) and global exception handler
- Unit tests: business rules + cache behavior

## How to run

### 1) Start Redis
```bash
docker compose up -d
```

### 2) Run API
```bash
dotnet restore
dotnet run --project Santander.HackerNewsApi
```

Swagger: `/swagger`

## Tests
```bash
dotnet test
```

## Notes / Assumptions
- “first n best stories”: takes the first n IDs returned by `beststories.json`, then fetches details and sorts by score desc.
- `commentCount` maps from `descendants` field in Hacker News items.

## Possible improvements
- Single-flight per cache key to avoid thundering herd on cold cache
- Health checks (Redis + upstream)
- Observability (OpenTelemetry metrics/tracing)
