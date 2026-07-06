
using System;
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

        [InventoryItemParameters(MaxHealth = 10, Arrmor = 10)]
        IronHelmet = 1004,

        [InventoryItemParameters(MaxHealth = 20, Arrmor = 20)]     
        IronChest = 1005,

        [InventoryItemParameters(MaxHealth = 5, Agility = 5, Stamina = 5, Arrmor = 5)]
        IronBoots = 1006,

        [InventoryItemParameters(Strength = 20)]
        IronSword = 1007,

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