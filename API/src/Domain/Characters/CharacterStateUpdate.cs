using ProjectX.Domain.Enums;

namespace ProjectX.Domain.Characters;

public sealed record CharacterStateUpdate(
    int? Health,
    int? MaxHealth,
    short? Strength,
    short? Dexterity,
    short? Speed,
    short? Intellect,
    short? Armor,
    InventoryItemEnum? HelmetType,
    InventoryItemEnum? ChestType,
    InventoryItemEnum? BootsType,
    InventoryItemEnum? WeaponType,
    InventoryItemEnum? AmmoType,
    int? AmmoCount);
