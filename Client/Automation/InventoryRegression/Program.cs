using System;
using System.Linq;
using Assets.Scripts.Areas.Character;
using Assets.Scripts.Areas.Character.Models;
using Assets.Scripts.Areas.Inventory.Enums;
using Assets.Scripts.Areas.Inventory.Models;
using Assets.Scripts.Areas.Inventory.Shared;
using Assets.Scripts.Areas.Inventory.Subscriptions;

const ulong clientId = 7;
var cases = new (InventoryItemEnum Type, Func<InventoryItemDto, AbstractGearUsableItem> Create)[]
{
    (InventoryItemEnum.IronHelmet, item => new HelmetUsableItem(item, "test-session", clientId)),
    (InventoryItemEnum.IronChest, item => new ChestUsableItem(item, "test-session", clientId)),
    (InventoryItemEnum.IronBoots, item => new BootsUsableItem(item, "test-session", clientId)),
    (InventoryItemEnum.IronSword, item => new WeaponUsableItem(item, "test-session", clientId)),
    (InventoryItemEnum.AmmoOil1, item => new AmmoUsableItem(item, "test-session", clientId))
};

foreach (var test in cases)
{
    var live = NewCharacter();
    if (test.Type == InventoryItemEnum.AmmoOil1)
    {
        live.WeaponType = InventoryItemEnum.IronSword;
    }

    UserManager.Instance.Characters[clientId] = live;
    var before = Equipment(live);
    var gear = test.Create(new InventoryItemDto { Type = test.Type, Count = 1 });

    Check(gear.TryUse(UsableItemFromEnum.Inventory), "Equipment request accepted for validation");

    var request = UpdateInventorySubscription.Instance.LastEvent;
    Check(request.Request.HealthOperationId.HasValue
        && request.Request.HealthOperationId != Guid.Empty, "Gear persistence has a receipt identifier");
    Check(Equipment(live) == before, "Pending gear must not affect combat state");
    request.OnRejected?.Invoke();
    Check(Equipment(live) == before, "Rejected ownership must leave state unchanged");

    // Simulate a successful later attempt, including live changes while awaiting API.
    Check(gear.TryUse(UsableItemFromEnum.Inventory), "Retry accepted");
    request = UpdateInventorySubscription.Instance.LastEvent;
    live.Health = 37;
    live.StrengthBuff = 20;
    UserManager.Instance.Characters.Remove(clientId);

    request.OnPersisted();

    Check(Equipment(live) != before, "Applied gear must update captured state even after despawn");
    Check(live.Health == 37 && live.StrengthBuff == 20, "Commit must preserve concurrent health and buffs");
    Check(live.MaxHealth == request.Request.CharacterUpdate.MaxHealth, "Persisted and active stats match");
}

var armed = NewCharacter();
armed.WeaponType = InventoryItemEnum.IronSword;
armed.AmmoType = InventoryItemEnum.AmmoOil1;
armed.AmmoCount = 4;
AbstractGearUsableItem.AddStats(armed, armed.WeaponType);
AbstractGearUsableItem.AddStats(armed, armed.AmmoType);
UserManager.Instance.Characters[clientId] = armed;
var armedBefore = Equipment(armed);

var bow = new WeaponUsableItem(new() { Type = InventoryItemEnum.IronBow, Count = 1 }, "test-session", clientId);
Check(bow.TryUse(UsableItemFromEnum.Inventory), "Weapon switch accepted");

var switchRequest = UpdateInventorySubscription.Instance.LastEvent;
Check(Equipment(armed) == armedBefore, "Pending switch must retain weapon and ammo bonuses");
Check(switchRequest.Request.Add.Any(x => x.Type == InventoryItemEnum.AmmoOil1 && x.Count == 4), "Incompatible ammo returned in the same transaction");

switchRequest.OnPersisted();

Check(armed.WeaponType == InventoryItemEnum.IronBow && armed.AmmoCount == 0, "Successful switch publishes weapon and ammo together");
var equippedBefore = Equipment(armed);
Check(bow.TryUse(UsableItemFromEnum.Gear), "Unequip accepted");
Check(Equipment(armed) == equippedBefore, "Pending unequip retains active gear");

UpdateInventorySubscription.Instance.LastEvent.OnPersisted();

Check(armed.WeaponType == InventoryItemEnum.WeaponTemplate, "Unequip applied after persistence");
foreach (var liveHealth in new[] { 120, 110, 85, 0 })
{
    var armored = NewCharacter();
    armored.ChestType = InventoryItemEnum.IronChest;
    AbstractGearUsableItem.AddStats(armored, armored.ChestType);
    armored.Health = armored.MaxHealth;
    UserManager.Instance.Characters[clientId] = armored;

    var chest = new ChestUsableItem(new() { Type = InventoryItemEnum.IronChest, Count = 1 }, "test-session", clientId);
    Check(chest.TryUse(UsableItemFromEnum.Gear), "Chest unequip accepted");

    var unequip = UpdateInventorySubscription.Instance.LastEvent;
    Check(unequip.Request.CharacterUpdate.Health == 100, "Inventory transaction must persist capped HP with the new maximum");
    Check(armored.Health == 120 && armored.MaxHealth == 120, "Pending unequip must not reduce live HP");
    unequip.OnRejected?.Invoke();
    Check(armored.Health == 120 && armored.MaxHealth == 120, "Rejected unequip must preserve HP and maximum");

    armored.Health = liveHealth;
    unequip.OnPersisted();

    Check(armored.MaxHealth == 100 && armored.Health == Math.Min(liveHealth, 100), "Commit caps current HP without healing damage or reviving a dead player");
}

foreach (var committed in new[] { false, true })
{
    var wounded = NewCharacter();
    wounded.Health = 50;
    UserManager.Instance.Characters[clientId] = wounded;

    var potion = new HealthPotionUsableItem(new() { Type = InventoryItemEnum.HealthPotion, Count = 1 }, "test-session", clientId);
    Check(potion.TryUse(UsableItemFromEnum.Inventory), "Potion accepted");

    var pendingPotion = UpdateInventorySubscription.Instance.LastEvent;
    Check(pendingPotion.Request.HealthOperationId.HasValue
        && pendingPotion.Request.HealthOperationId != Guid.Empty, "Potion has a stable operation identifier");
    Check(pendingPotion.Request.CharacterUpdate.Health == 70, "Atomic potion request includes healing");
    Check(wounded.Health == 50, "Pending potion does not publish healing");

    wounded.Health -= 10;
    UserManager.Instance.Characters.Remove(clientId);

    if (committed)
    {
        pendingPotion.OnPersisted();
    }

    Check(wounded.Health == (committed ? 60 : 40),
        "Lost response recovery preserves damage after despawn for committed and rejected potion");
}

Console.WriteLine("GEAR_COMMIT_REGRESSION_PASSED: gear, HP clamping and uncertain potion recovery with concurrent damage after despawn.");

await AmmoRegression.RunAsync();

static CharacterDto NewCharacter() => new()
{
    Health = 100,
    MaxHealth = 100,
    HelmetType = InventoryItemEnum.HelmetTemplate,
    ChestType = InventoryItemEnum.ChestTemplate,
    BootsType = InventoryItemEnum.BootsTemplate,
    WeaponType = InventoryItemEnum.WeaponTemplate,
    AmmoType = InventoryItemEnum.AmmoTemplate
};

static string Equipment(CharacterDto value) =>
    $"{value.MaxHealth}/{value.Strength}/{value.Dexterity}/{value.Speed}/{value.Intellect}/{value.Armor}/" +
    $"{value.HelmetType}/{value.ChestType}/{value.BootsType}/{value.WeaponType}/{value.AmmoType}/{value.AmmoCount}";

static void Check(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}
