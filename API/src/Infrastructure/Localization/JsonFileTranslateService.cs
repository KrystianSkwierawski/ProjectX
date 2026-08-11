using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Domain.Enums;

namespace ProjectX.Infrastructure.Localization;

public sealed class JsonFileTranslateService : ITranslateService
{
    private static readonly TimeSpan CacheLifetime = TimeSpan.FromHours(1);

    private readonly IMemoryCache _memoryCache;
    private readonly ICurrentUserService _currentUserService;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly ILogger<JsonFileTranslateService> _logger;

    public JsonFileTranslateService(
        IMemoryCache memoryCache,
        ICurrentUserService currentUserService,
        IHostEnvironment hostEnvironment,
        ILogger<JsonFileTranslateService> logger)
    {
        _memoryCache = memoryCache;
        _currentUserService = currentUserService;
        _hostEnvironment = hostEnvironment;
        _logger = logger;
    }

    public string GetByKey(string key, LanguageEnum? language = null)
    {
        return Enum.TryParse<TranslateKeyEnum>(key, out var enumKey)
            ? GetByKey(enumKey, language)
            : string.Empty;
    }

    public string GetByKey(TranslateKeyEnum key, LanguageEnum? language = null)
    {
        var selectedLanguage = language ?? _currentUserService.Language;
        var translations = GetTranslations(selectedLanguage);

        if (translations.TryGetValue(key.ToString(), out var translation))
        {
            return translation;
        }

        _logger.LogWarning("Translation was not found. Key: {Key}, Language: {Language}", key, selectedLanguage);
        return string.Empty;
    }

    private IReadOnlyDictionary<string, string> GetTranslations(LanguageEnum language)
    {
        return _memoryCache.GetOrCreate($"translations:{language}", entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheLifetime;

            var path = Path.Combine(_hostEnvironment.ContentRootPath, "wwwroot", "i18n", $"{language}.json");

            try
            {
                using var stream = File.OpenRead(path);
                return JsonSerializer.Deserialize<Dictionary<string, string>>(stream) ?? new Dictionary<string, string>();
            }
            catch (Exception exception) when (exception is IOException or JsonException or UnauthorizedAccessException)
            {
                _logger.LogError(exception, "Translations could not be loaded from {Path}", path);
                return new Dictionary<string, string>();
            }
        }) ?? new Dictionary<string, string>();
    }
}
