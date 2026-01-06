using Microsoft.Extensions.Caching.Memory;
using ProjectX.Domain.Enums;

namespace ProjectX.Application.Extensions;

public static class IMemoryCacheExtensions
{
    private static readonly Serilog.ILogger Log = Serilog.Log.ForContext(typeof(IMemoryCacheExtensions));

    public static async Task<TItem> GetOrCreateAsync<TItem>(this IMemoryCache cache, CacheKeyEnum keyEnum, Func<ICacheEntry, Task<TItem>> factory, params object?[] args)
    {
        var parameters = keyEnum.GetCacheKeyParametersAttribute();

        var key = parameters.Format;

        if (args != null)
        {
            key = string.Format(key, args);
        }

        Log.Verbose("GetOrCreateAsync -> Key: {0}, ExpiryInSeconds: {1}", key, parameters.ExpiryInSeconds);

        return await cache.GetOrCreateAsync(key, factory, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(parameters.ExpiryInSeconds)
        }) ?? throw new ArgumentNullException(nameof(keyEnum));
    }
}
