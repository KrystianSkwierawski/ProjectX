
namespace Assets.Scripts.Areas.Inventory.Enums
{
    public enum InventoryItemEnum
    {
        None,

        #region Common

        Can = 100,
        Currency = 101,
        Xp = 102,

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
    }
}