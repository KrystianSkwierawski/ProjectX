namespace ProjectX.Domain.Enums;
public enum InventoryItemEnum
{
    None,

    #region Common

    Can = 100,

    #endregion

    #region Food

    Fish = 200,
    CookedFish = 201,
    Rice = 202,
    Sushi = 203,

    #endregion

    #region Ore

    PurpleOre = 300,
    WhiteOre = 301,
    CopperOre = 302,
    BlackOre = 303,
    Chamomile = 304,
    Wood = 305,

    #endregion
}