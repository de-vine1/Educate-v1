using Microsoft.Extensions.Caching.Memory;

namespace Educate.Infrastructure.Services;

public class RateLimitingService
{
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _windowDuration = TimeSpan.FromMinutes(15);
    private readonly int _maxAttempts = 3;

    public RateLimitingService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public bool IsRateLimited(string key)
    {
        var cacheKey = $"rate_limit_{key}";
        var attempts = _cache.Get<int>(cacheKey);

        if (attempts >= _maxAttempts)
        {
            return true;
        }

        _cache.Set(cacheKey, attempts + 1, _windowDuration);
        return false;
    }
}
