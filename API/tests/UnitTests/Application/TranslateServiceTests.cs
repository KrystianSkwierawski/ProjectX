using Microsoft.Extensions.Caching.Memory;
using Moq;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Application.Translate;
using ProjectX.Domain.Enums;

namespace ProjectX.UnitTests.Application;

public class TranslateServiceTests
{
    private readonly ITranslateService _translateService;

    public TranslateServiceTests()
    {
        var memoryCache = new MemoryCache(new MemoryCacheOptions());

        var currentUserServiceMock = new Mock<ICurrentUserService>();

        _translateService = new TranslateService(memoryCache, currentUserServiceMock.Object);
    }

    [Theory]
    [MemberData(nameof(TheoryData))]
    public void GetByKeyTest(TranslateKeyEnum key, LanguageEnum language)
    {
        var result = _translateService.GetByKey(key, language);

        Assert.NotNull(result);

        if (key != TranslateKeyEnum.Empty)
            Assert.NotEmpty(result);
    }

    public static TheoryData<TranslateKeyEnum, LanguageEnum> TheoryData
    {
        get
        {
            var result = new TheoryData<TranslateKeyEnum, LanguageEnum>();

            foreach (var key in Enum.GetValues<TranslateKeyEnum>())
                foreach (var language in Enum.GetValues<LanguageEnum>())
                {
                    result.Add(key, language);
                }

            return result;
        }
    }
}
