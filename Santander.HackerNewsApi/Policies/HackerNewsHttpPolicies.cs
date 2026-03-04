using System.Net;
using Polly;
using Polly.Extensions.Http;

namespace Santander.HackerNewsApi.Policies;

/// <summary>
/// Provides reusable HTTP resilience policies for interacting with the Hacker News API, including retry, circuit
/// breaker, and timeout strategies.
/// </summary>
public static class HackerNewsHttpPolicies
{
    public static IAsyncPolicy<HttpResponseMessage> RetryPolicy(ILogger logger)
        => HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(r => r.StatusCode == HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt => TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempt)),
                onRetry: (outcome, delay, attempt, _) =>
                {
                    logger.LogWarning(
                        "Retry {Attempt} after {Delay}ms (status={StatusCode})",
                        attempt,
                        delay.TotalMilliseconds,
                        outcome.Result?.StatusCode);
                });

    public static IAsyncPolicy<HttpResponseMessage> CircuitBreakerPolicy(ILogger logger)
        => HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(r => r.StatusCode == HttpStatusCode.TooManyRequests)
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 20,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (outcome, breakDelay) =>
                {
                    logger.LogWarning("Circuit open for {BreakDelay}s", breakDelay.TotalSeconds);
                },
                onReset: () => logger.LogInformation("Circuit reset"),
                onHalfOpen: () => logger.LogInformation("Circuit half-open"));

    public static IAsyncPolicy<HttpResponseMessage> TimeoutPolicy()
        => Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(3));
}
