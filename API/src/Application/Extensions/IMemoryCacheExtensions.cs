using Microsoft.Extensions.Caching.Memory;
using ProjectX.Domain.Enums;

namespace ProjectX.Application.Extensions;

public static class IMemoryCacheExtensions
{
    private static readonly Serilog.ILogger Log = Serilog.Log.ForContext(typeof(IMemoryCacheExtensions));

    public static TItem GetOrCreate<TItem>(this IMemoryCache memoryCache, CacheKeyEnum keyEnum, Func<ICacheEntry, TItem> factory, params object?[] args)
    {
        var parameters = keyEnum.GetCacheKeyParametersAttribute();

        var key = parameters.Format;

        if (args != null)
        {
            key = string.Format(key, args);
        }

        Log.Verbose("GetOrCreateAsync -> Key: {0}, ExpiryInSeconds: {1}", key, parameters.ExpiryInSeconds);

        return memoryCache.GetOrCreate(key, factory, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(parameters.ExpiryInSeconds)
        }) ?? throw new ArgumentNullException(nameof(keyEnum));
    }

    public static async Task<TItem> GetOrCreateAsync<TItem>(this IMemoryCache memoryCache, CacheKeyEnum keyEnum, Func<ICacheEntry, Task<TItem>> factory, params object?[] args)
    {
        var parameters = keyEnum.GetCacheKeyParametersAttribute();

        var key = parameters.Format;

        if (args != null)
        {
            key = string.Format(key, args);
        }

        Log.Verbose("GetOrCreateAsync -> Key: {0}, ExpiryInSeconds: {1}", key, parameters.ExpiryInSeconds);

        return await memoryCache.GetOrCreateAsync(key, factory, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(parameters.ExpiryInSeconds)
        }) ?? throw new ArgumentNullException(nameof(keyEnum));
    }
}
