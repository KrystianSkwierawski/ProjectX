using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Domain.Enums;
using ProjectX.Infrastructure.Localization;

namespace ProjectX.Web.AcceptanceTests.Localization;

public class JsonFileTranslateServiceTests
{
    private readonly ITranslateService _translateService;

    public JsonFileTranslateServiceTests()
    {
        var currentUser = new Mock<ICurrentUserService>();
        var hostEnvironment = new Mock<IHostEnvironment>();
        hostEnvironment.SetupGet(environment => environment.ContentRootPath).Returns(AppContext.BaseDirectory);

        _translateService = new JsonFileTranslateService(
            new MemoryCache(new MemoryCacheOptions()),
            currentUser.Object,
            hostEnvironment.Object,
            NullLogger<JsonFileTranslateService>.Instance);
    }

    [Theory]
    [MemberData(nameof(TheoryData))]
    public void GetByKey_ReturnsEveryDeclaredTranslation(TranslateKeyEnum key, LanguageEnum language)
    {
        var result = _translateService.GetByKey(key, language);

        Assert.NotNull(result);

        if (key != TranslateKeyEnum.Empty)
        {
            Assert.NotEmpty(result);
        }
    }

    public static TheoryData<TranslateKeyEnum, LanguageEnum> TheoryData
    {
        get
        {
            var result = new TheoryData<TranslateKeyEnum, LanguageEnum>();

            foreach (var key in Enum.GetValues<TranslateKeyEnum>())
            {
                foreach (var language in Enum.GetValues<LanguageEnum>())
                {
                    result.Add(key, language);
                }
            }

            return result;
        }
    }
}
