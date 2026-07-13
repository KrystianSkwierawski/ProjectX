namespace ProjectX.Domain.Enums;

public enum InventoryItemEnum
{
    None,

    #region Common

    Can = 100,
    Currency = 101,

    #endregion

    #region Fishing&Cooking

    Fish = 200,
    CookedFish = 201,
    Rice = 202,
    Sushi = 203,

    #endregion

    #region Mining&Blacksmithing

    PurpleOre = 300,
    WhiteOre = 301,
    CopperOre = 302,
    BlackOre = 303,
    PurpleBar = 304,
    WhiteBar = 305,
    CopperBar = 306,
    BlackBar = 307,

    #endregion

    #region Lumberjack

    Wood = 400,

    #endregion

    #region Herbalism&Alchemy

    Chamomile = 500,
    HealthPotion = 501,

    #endregion

    #region Gear

    HelmetTemplate = 1000,
    ChestTemplate = 1001,
    BootsTemplate = 1002,
    WeaponTemplate = 1003,
    IronHelmet = 1004,
    IronChest = 1005,
    IronBoots = 1006,
    IronSword = 1007,
    AmmoTemplate = 1008,
    AmmoArrow1 = 1009,
    AmmoArrow2 = 1010,
    AmmoArrow3 = 1011,
    AmmoRune1 = 1012,
    AmmoRune2 = 1013,
    AmmoRune3 = 1014,
    AmmoFeather1 = 1015,
    AmmoFeather2 = 1016,
    AmmoFeather3 = 1017,
    AmmoOil1 = 1018,
    AmmoOil2 = 1019,
    AmmoOil3 = 1020,

    #endregion
}
