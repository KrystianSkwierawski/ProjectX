using System.Reflection;
using ProjectX.Application.Common.Attributes;

namespace ProjectX.Domain.Enums;
public enum CacheKeyEnum
{
    [CacheKeyParameters(
        Format = "Common",
        ExpiryInSeconds = 3600
    )]
    Common,

    [CacheKeyParameters(
      Format = "Quest_{0}_{1}",
      ExpiryInSeconds = 3600
    )]
    Quest,

    [CacheKeyParameters(
      Format = "Quests_{0}",
      ExpiryInSeconds = 3600
    )]
    Quests,
}

public static class CacheKeyEnumExtensions
{
    public static CacheKeyParametersAttribute GetCacheKeyParametersAttribute(this CacheKeyEnum value)
    {
        var member = value
            .GetType()
            .GetMember(value.ToString())
            .First();

        return member.GetCustomAttribute<CacheKeyParametersAttribute>() ?? throw new ArgumentNullException(nameof(value));
    }
}
