using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json.Linq;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Application.Extensions;
using ProjectX.Domain.Enums;

namespace ProjectX.Application.Translate;
public class TranslateService : ITranslateService
{
    private static readonly Serilog.ILogger Log = Serilog.Log.ForContext<TranslateService>();

    private readonly IMemoryCache _memoryCache;
    private readonly ICurrentUserService _currentUserService;

    public TranslateService(IMemoryCache memoryCache, ICurrentUserService currentUserService)
    {
        _memoryCache = memoryCache;
        _currentUserService = currentUserService;
    }

    public string GetByKey(TranslateKeyEnum key, LanguageEnum? language = null)
    {
        language ??= _currentUserService.Language;

        return _memoryCache.GetOrCreate(CacheKeyEnum.Translate, (ICacheEntry entry) =>
        {
            try
            {
                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "i18n", $"{language}.json");

                using var reader = new StreamReader(path);

                var json = reader.ReadToEnd();

                var obj = JObject.Parse(json);

                var keyString = key.ToString();

                var result = obj.SelectToken(keyString);

                if (result == null)
                {
                    Log.Warning("Not found translate. Key: {0}, Value: {1}, Language: {2}", keyString, result, language);

                    return string.Empty;
                }

                Log.Debug("Found translate. Key: {0}, Value: {1}, Language: {2}", keyString, result, language);

                return result.ToString();
            }
            catch (Exception ex)
            {
                Log.Error(ex, ex.Message);

                return string.Empty;
            }
        }, key, language);
    }
}
