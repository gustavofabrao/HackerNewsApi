using Microsoft.Extensions.Caching.Distributed;

namespace Santander.HackerNewsApi.Caching
{
    /// <summary>
    /// Provides a resilient implementation of <see cref="IDistributedCache"/> that logs failures and suppresses
    /// exceptions when the underlying distributed cache is unavailable.
    /// </summary>
    public class SafeDistributedCache : IDistributedCache
    {
        private readonly IDistributedCache _inner;
        private readonly ILogger<SafeDistributedCache> _logger;

        public SafeDistributedCache(IDistributedCache inner, ILogger<SafeDistributedCache> logger)
        {
            _inner = inner;
            _logger = logger;
        }

        public byte[] Get(string key)
        {
            try { return _inner.Get(key); }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis unavailable for Get({Key})", key);
                return null;
            }
        }

        public async Task<byte[]> GetAsync(string key, CancellationToken token = default)
        {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
            cts.CancelAfter(TimeSpan.FromMilliseconds(300));
            try { return await _inner.GetAsync(key, cts.Token); }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis unavailable for GetAsync({Key})", key);
                return null;
            }
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            try { _inner.Set(key, value, options); }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis unavailable for Set({Key})", key);
            }
        }

        public async Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
        {
            try { await _inner.SetAsync(key, value, options, token); }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis unavailable for SetAsync({Key})", key);
            }
        }

        public void Refresh(string key)
        {
            try { _inner.Refresh(key); }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis unavailable for Refresh({Key})", key);
            }
        }

        public async Task RefreshAsync(string key, CancellationToken token = default)
        {
            try { await _inner.RefreshAsync(key, token); }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis unavailable for RefreshAsync({Key})", key);
            }
        }

        public void Remove(string key)
        {
            try { _inner.Remove(key); }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis unavailable for Remove({Key})", key);
            }
        }

        public async Task RemoveAsync(string key, CancellationToken token = default)
        {
            try { await _inner.RemoveAsync(key, token); }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis unavailable for RemoveAsync({Key})", key);
            }
        }
    }
}