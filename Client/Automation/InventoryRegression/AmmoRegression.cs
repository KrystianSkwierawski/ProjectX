using System;
using System.Threading;
using System.Threading.Tasks;
using Assets.Scripts.Areas.Character;
using Assets.Scripts.Areas.Character.Models;
using Assets.Scripts.Areas.Inventory.Enums;
using Assets.Scripts.Areas.Inventory.Models;
using Assets.Scripts.Areas.Inventory.Shared;

internal static class AmmoRegression
{
    internal static async Task RunAsync()
    {
        foreach (var unequip in new[] { true, false })
        {
            using var gate = new SemaphoreSlim(0, 1);
            var character = Equipped(InventoryItemEnum.AmmoOil1, 4);
            float damage = 0;
            UpdateCharacterInventoryCommand saved = null;
            var pending = AmmoHitCoordinator.ResolveAsync(gate, () => character, current =>
            {
                damage = current.ApplyWeaponDamage(10);

                return true;
            }, false, (request, _) =>
            {
                saved = request;

                return Applied();
            }, () => { }, Fail, CancellationToken.None);

            Check(damage == 0 && saved == null, "Hit must wait before calculating ammo bonus");

            // A pending equipment transaction commits before the hit acquires the gate.
            character = Equipped(unequip ? InventoryItemEnum.AmmoTemplate : InventoryItemEnum.AmmoOil3, unequip ? 0 : 8);
            gate.Release();

            await pending.WaitAsync(TimeSpan.FromSeconds(10));

            Check(Math.Abs(damage - (unequip ? 12f : 13.5f)) < .001f,
                "Damage must use the same committed ammo state as its cost");
            Check(unequip ? saved == null : saved?.CharacterUpdate.AmmoCount == 7,
                "Unequipped ammo has no bonus/cost; switched ammo has its own bonus and cost");
        }

        using (var gate = new SemaphoreSlim(1, 1))
        {
            var character = Equipped(InventoryItemEnum.AmmoFeather1, 1);
            var damage = 0;
            var notifications = 0;

            await AmmoHitCoordinator.ResolveAsync(gate, () => character, current =>
            {
                damage = current.ApplyArmor(100);

                return true;
            }, true, (request, _) =>
            {
                Check(request.CharacterUpdate.AmmoType == InventoryItemEnum.AmmoTemplate
                    && request.CharacterUpdate.AmmoCount == 0 && request.CharacterUpdate.Armor == 0,
                    "Final defensive charge persists ammo and stat removal together");
                Check(!request.CharacterUpdate.Health.HasValue, "Ammo receipt must not overwrite concurrent HP");

                return Applied();
            }, () => notifications++, Fail, CancellationToken.None);

            Check(damage == 95 && character.Armor == 0 && notifications == 1,
                "Final defensive charge protects this hit and expires exactly once");
        }

        using (var gate = new SemaphoreSlim(1, 1))
        {
            var character = Equipped(InventoryItemEnum.AmmoOil1, 1);
            var firstFailure = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var retryAllowed = new TaskCompletionSource<UpdateCharacterInventoryDto>(TaskCreationOptions.RunContinuationsAsynchronously);
            UpdateCharacterInventoryCommand original = null;
            var attempts = 0;
            var notifications = 0;
            var connected = true;
            var pending = AmmoHitCoordinator.ResolveAsync(gate, () => connected ? character : null, current => true,
                false, (request, _) =>
                {
                    attempts++;

                    if (attempts == 1)
                    {
                        original = request;

                        return Task.FromException<UpdateCharacterInventoryDto>(new TimeoutException());
                    }

                    Check(ReferenceEquals(original, request) && request.HealthOperationId != Guid.Empty
                        && request.CharacterUpdate.AmmoCount == 0 && request.CharacterUpdate.Strength == 20,
                        "Uncertain save must replay the identical hit payload and receipt ID");

                    return retryAllowed.Task;
                }, () => notifications++, _ => firstFailure.TrySetResult(), CancellationToken.None);

            await firstFailure.Task.WaitAsync(TimeSpan.FromSeconds(10));

            var laterEquipment = gate.WaitAsync();

            Check(!pending.IsCompleted && !laterEquipment.IsCompleted,
                "Timeout must keep subsequent equipment changes behind the gate");

            connected = false;
            character.Health = 37;
            character.StrengthBuff = 20;
            retryAllowed.SetResult(new UpdateCharacterInventoryDto { Status = UpdateCharacterInventoryStatusEnum.Applied });

            await pending.WaitAsync(TimeSpan.FromSeconds(10));
            await laterEquipment.WaitAsync(TimeSpan.FromSeconds(10));

            Check(attempts == 2 && notifications == 1 && character.AmmoCount == 0
                && character.Health == 37 && character.StrengthBuff == 20,
                "Accepted hit recovers after despawn without duplicate cost, RPC or stale HP/buffs");

            gate.Release();
        }

        using (var gate = new SemaphoreSlim(1, 1))
        {
            var character = Equipped(InventoryItemEnum.AmmoOil1, 2);

            await AmmoHitCoordinator.ResolveAsync(gate, () => character, _ => false, false,
                (_, _) => throw new InvalidOperationException("Dodged/invalid hit reached persistence"),
                () => throw new InvalidOperationException("Dodged/invalid hit consumed ammo"), Fail, CancellationToken.None);

            await AmmoHitCoordinator.ResolveAsync(gate, () => null,
                _ => throw new InvalidOperationException("Despawned player received queued hit"), false,
                (_, _) => throw new InvalidOperationException("Unaccepted hit persisted after despawn"),
                () => { }, Fail, CancellationToken.None);

            Check(character.AmmoCount == 2, "Invalid, dodged and unaccepted despawned hits do not consume ammo");
        }

        Console.WriteLine("AMMO_HIT_REGRESSION_PASSED: equipment ordering, final defensive charge, timeout barrier, immutable replay and despawn.");
    }

    private static CharacterDto Equipped(InventoryItemEnum ammo, int count)
    {
        var character = new CharacterDto
        {
            Health = 100,
            MaxHealth = 100,
            Strength = 20,
            WeaponType = InventoryItemEnum.IronSword,
            AmmoType = ammo,
            AmmoCount = count
        };

        if (ammo.IsAmmo())
        {
            AbstractGearUsableItem.AddStats(character, ammo);
        }

        return character;
    }

    private static Task<UpdateCharacterInventoryDto> Applied() => Task.FromResult(
        new UpdateCharacterInventoryDto { Status = UpdateCharacterInventoryStatusEnum.Applied });

    private static void Fail(Exception exception) => throw new InvalidOperationException("Unexpected recovery", exception);

    private static void Check(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
