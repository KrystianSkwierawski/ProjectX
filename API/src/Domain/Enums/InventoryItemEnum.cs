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
    AmmoArrow = 1009,
    AmmoRune = 1010,
    AmmoFeather = 1011,
    AmmoOil = 1012,

    #endregion
}
