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
        AmmoTemplate = 1004,

        [InventoryItemParameters(MaxHealth = 10, Armor = 10)]
        IronHelmet = 1005,

        [InventoryItemParameters(MaxHealth = 20, Armor = 20)]
        IronChest = 1006,

        [InventoryItemParameters(MaxHealth = 5, Speed = 80, Armor = 5)]
        IronBoots = 1007,

        [InventoryItemParameters(WeaponCategory = WeaponCategoryEnum.Sword, Strength = 20)]
        IronSword = 1008,

        [InventoryItemParameters(WeaponCategory = WeaponCategoryEnum.Bow, Dexterity = 5)]
        AmmoArrow1 = 1009,

        [InventoryItemParameters(WeaponCategory = WeaponCategoryEnum.Bow, Dexterity = 10)]
        AmmoArrow2 = 1010,

        [InventoryItemParameters(WeaponCategory = WeaponCategoryEnum.Bow, Dexterity = 15)]
        AmmoArrow3 = 1011,

        [InventoryItemParameters(WeaponCategory = WeaponCategoryEnum.Wand, Intellect = 5)]
        AmmoRune1 = 1012,

        [InventoryItemParameters(WeaponCategory = WeaponCategoryEnum.Wand, Intellect = 10)]
        AmmoRune2 = 1013,

        [InventoryItemParameters(WeaponCategory = WeaponCategoryEnum.Wand, Intellect = 15)]
        AmmoRune3 = 1014,

        [InventoryItemParameters(WeaponCategory = WeaponCategoryEnum.Sword, Armor = 5)]
        AmmoFeather1 = 1015,

        [InventoryItemParameters(WeaponCategory = WeaponCategoryEnum.Sword, Armor = 10)]
        AmmoFeather2 = 1016,

        [InventoryItemParameters(WeaponCategory = WeaponCategoryEnum.Sword, Armor = 15)]
        AmmoFeather3 = 1017,

        [InventoryItemParameters(WeaponCategory = WeaponCategoryEnum.Sword, Strength = 5)]
        AmmoOil1 = 1018,

        [InventoryItemParameters(WeaponCategory = WeaponCategoryEnum.Sword, Strength = 10)]
        AmmoOil2 = 1019,

        [InventoryItemParameters(WeaponCategory = WeaponCategoryEnum.Sword, Strength = 15)]
        AmmoOil3 = 1020,

        [InventoryItemParameters(WeaponCategory = WeaponCategoryEnum.Wand, Intellect = 20)]
        IronWand = 1021,

        [InventoryItemParameters(WeaponCategory = WeaponCategoryEnum.Bow, Dexterity = 20)]
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

        public static bool IsCompatibleWithWeapon(this InventoryItemEnum ammoType, InventoryItemEnum weaponType)
        {
            if (!ammoType.IsAmmo() || !weaponType.IsWeapon())
            {
                return false;
            }

            var ammoParameters = ammoType.GetInventoryItemParametersAttribute();
            var weaponParameters = weaponType.GetInventoryItemParametersAttribute();

            return ammoParameters != null
                && weaponParameters != null
                && ammoParameters.WeaponCategory != WeaponCategoryEnum.None
                && ammoParameters.WeaponCategory == weaponParameters.WeaponCategory;
        }
    }

}
