using System.Reflection;
using ProjectX.Domain.Attributes;

namespace ProjectX.Domain.Enums;

public enum HideoutBuildingEnum
{
    None = 0,

    [HideoutBuildingParameters([InventoryItemEnum.Chamomile], [5], BuildTime = 5)]
    ChamomileFarm = 1
}

public static class HideoutBuildingEnumExtensions
{
    public static HideoutBuildingParametersAttribute GetParameters(this HideoutBuildingEnum value)
    {
        var member = value
            .GetType()
            .GetMember(value.ToString())
            .First();

        return member.GetCustomAttribute<HideoutBuildingParametersAttribute>() ?? throw new ArgumentNullException(nameof(value));
    }
}
