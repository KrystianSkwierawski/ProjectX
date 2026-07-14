using System.Linq;
using System.Reflection;
using Assets.Scripts.Areas.Shared.Attributes;

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

        #region Gear

        HelmetTemplate = 1000,
        ChestTemplate = 1001,
        BootsTemplate = 1002,
        WeaponTemplate = 1003,

        [InventoryItemParameters(MaxHealth = 10, Armor = 10)]
        IronHelmet = 1004,

        [InventoryItemParameters(MaxHealth = 20, Armor = 20)]
        IronChest = 1005,

        [InventoryItemParameters(MaxHealth = 5, Speed = 80, Armor = 5)]
        IronBoots = 1006,

        [InventoryItemParameters(Strength = 20)]
        IronSword = 1007,

        AmmoTemplate = 1008,

        [InventoryItemParameters(Dexterity = 5)]
        AmmoArrow1 = 1009,

        [InventoryItemParameters(Dexterity = 10)]
        AmmoArrow2 = 1010,

        [InventoryItemParameters(Dexterity = 15)]
        AmmoArrow3 = 1011,

        [InventoryItemParameters(Intellect = 5)]
        AmmoRune1 = 1012,

        [InventoryItemParameters(Intellect = 10)]
        AmmoRune2 = 1013,

        [InventoryItemParameters(Intellect = 15)]
        AmmoRune3 = 1014,

        [InventoryItemParameters(Armor = 5)]
        AmmoFeather1 = 1015,

        [InventoryItemParameters(Armor = 10)]
        AmmoFeather2 = 1016,

        [InventoryItemParameters(Armor = 15)]
        AmmoFeather3 = 1017,

        [InventoryItemParameters(Strength = 5)]
        AmmoOil1 = 1018,

        [InventoryItemParameters(Strength = 10)]
        AmmoOil2 = 1019,

        [InventoryItemParameters(Strength = 15)]
        AmmoOil3 = 1020,

        [InventoryItemParameters(Intellect = 20)]
        IronWand = 1021,

        [InventoryItemParameters(Dexterity = 20)]
        IronBow = 1022,

        #endregion
    }

    public static class InventoryItemEnumExtensions
    {
        public static InventoryItemParametersAttribute GetInventoryItemParametersAttribute(this InventoryItemEnum value)
        {
            var member = value
                .GetType()
                .GetMember(value.ToString())
                .First();

            return member.GetCustomAttribute<InventoryItemParametersAttribute>();
        }

        public static bool IsAmmo(this InventoryItemEnum value)
        {
            return value is InventoryItemEnum.AmmoArrow1
                or InventoryItemEnum.AmmoArrow2
                or InventoryItemEnum.AmmoArrow3
                or InventoryItemEnum.AmmoRune1
                or InventoryItemEnum.AmmoRune2
                or InventoryItemEnum.AmmoRune3
                or InventoryItemEnum.AmmoFeather1
                or InventoryItemEnum.AmmoFeather2
                or InventoryItemEnum.AmmoFeather3
                or InventoryItemEnum.AmmoOil1
                or InventoryItemEnum.AmmoOil2
                or InventoryItemEnum.AmmoOil3;
        }

        public static bool IsWeapon(this InventoryItemEnum value)
        {
            return value is InventoryItemEnum.IronSword
                or InventoryItemEnum.IronWand
                or InventoryItemEnum.IronBow;
        }
    }

}
