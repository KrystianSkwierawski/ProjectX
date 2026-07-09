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

        [InventoryItemParameters(MaxHealth = 5, Dexterity = 5, Speed = 5, Armor = 5)]
        IronBoots = 1006,

        [InventoryItemParameters(Strength = 20)]
        IronSword = 1007,

        AmmoTemplate = 1008,

        [InventoryItemParameters(Strength = 5)]
        AmmoArrow = 1009,

        [InventoryItemParameters(Strength = 5, Intellect = 1)]
        AmmoRune = 1010,

        [InventoryItemParameters(Armor = 5)]
        AmmoFeather = 1011,

        [InventoryItemParameters(Strength = 5)]
        AmmoOil = 1012,

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
    }

}
