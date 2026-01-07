using ProjectX.Domain.Enums;

namespace ProjectX.Application.Common.Interfaces;
public interface ITranslateService
{
    string GetByKey(string key, LanguageEnum? language = null);
}
