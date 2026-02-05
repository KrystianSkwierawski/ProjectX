using ProjectX.Domain.Enums;

namespace ProjectX.Application.Common.Interfaces;
public interface ITranslateService
{
    string GetByKey(TranslateKeyEnum key, LanguageEnum? language = null);
}
