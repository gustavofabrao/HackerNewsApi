# Hacker News Best Stories API

ASP.NET Core (.NET 8) REST API that returns the first **n best stories** from Hacker News, sorted by score desc.

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

## Possible Future Improvements
- **Authentication and authorization**  
  Implement API security using OAuth2 or API keys to control access and protect the service.
- **Extended test coverage**  
  Add more unit tests covering edge cases and resilience scenarios, and possibly end-to-end tests validating the full request flow.
- **Health checks**  
  Expose health endpoints to monitor dependencies such as Redis and the Hacker News upstream API.
- **Single-flight and stale-while-revalidate caching**  
  Prevent multiple concurrent requests from fetching the same upstream resource when the cache is cold, and allow serving stale data while refreshing the cache in the background.
- **Rate limiting**  
  Apply request rate limiting to protect the API from abuse and reduce the risk of overloading the upstream Hacker News service.
- **Observability and metrics**  
  Add structured metrics and tracing (e.g OpenTelemetry) to monitor cache performance, upstream latency, retries, and circuit breaker behavior.
