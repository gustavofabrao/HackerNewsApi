using Microsoft.Extensions.Caching.Distributed;
using Santander.HackerNewsApi.Caching;
using Santander.HackerNewsApi.Middleware;
using Santander.HackerNewsApi.Policies;
using Santander.HackerNewsApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// L1 cache
builder.Services.AddMemoryCache();

// L2 cache with Redis
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration["Redis:ConnectionString"];
    options.InstanceName = builder.Configuration["Redis:InstanceName"];
});

builder.Services.AddTransient<IDistributedCache>(sp =>
{
    var inner = new Microsoft.Extensions.Caching.StackExchangeRedis.RedisCache(
        sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<Microsoft.Extensions.Caching.StackExchangeRedis.RedisCacheOptions>>()
    );
    var logger = sp.GetRequiredService<ILogger<SafeDistributedCache>>();
    return new SafeDistributedCache(inner, logger);
});

// HttpClient with Polly resilience policies
builder.Services.AddHttpClient<IHackerNewsClient, HackerNewsClient>(client =>
{
    client.BaseAddress = new Uri("https://hacker-news.firebaseio.com/");
    client.Timeout = TimeSpan.FromSeconds(10);
})
.AddPolicyHandler((sp, _) =>
{
    var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("Polly.HackerNews");
    return HackerNewsHttpPolicies.RetryPolicy(logger);
})
.AddPolicyHandler((sp, _) =>
{
    var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("Polly.HackerNews");
    return HackerNewsHttpPolicies.CircuitBreakerPolicy(logger);
})
.AddPolicyHandler(_ => HackerNewsHttpPolicies.TimeoutPolicy());

builder.Services.AddScoped<IHackerNewsService, HackerNewsService>();

var app = builder.Build();

// Midlewares
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();

