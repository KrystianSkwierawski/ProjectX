using System;
using System.Threading;
using System.Threading.Tasks;
using Assets.Scripts.Areas.Character.Models;
using Assets.Scripts.Areas.Inventory.Enums;
using Assets.Scripts.Areas.Inventory.Models;
using Assets.Scripts.Areas.Inventory.Shared;

namespace Assets.Scripts.Areas.Character
{
    internal static class AmmoHitCoordinator
    {
        internal static async Task ResolveAsync(
            SemaphoreSlim equipmentGate,
            Func<CharacterDto> getCurrentCharacter,
            Func<CharacterDto, bool> applyHit,
            bool defensiveAmmo,
            Func<UpdateCharacterInventoryCommand, CancellationToken, Task<UpdateCharacterInventoryDto>> persist,
            Action onConsumed,
            Action<Exception> onUncertain,
            CancellationToken operationToken)
        {
            await equipmentGate.WaitAsync(operationToken);

            try
            {
                var character = getCurrentCharacter();

                if (character == null || !applyHit(character))
                {
                    return;
                }

                if (!character.AmmoType.IsAmmo() || character.AmmoCount <= 0
                    || character.AmmoType.IsArmorAmmo() != defensiveAmmo)
                {
                    return;
                }

                character.AmmoCount--;

                if (character.AmmoCount == 0)
                {
                    AbstractGearUsableItem.RemoveStats(character, character.AmmoType);
                    character.AmmoType = InventoryItemEnum.AmmoTemplate;
                }

                // This immutable request belongs to this hit, including its final-charge stat removal.
                var request = new UpdateCharacterInventoryCommand
                {
                    HealthOperationId = Guid.NewGuid(),
                    CharacterUpdate = new UpdateCharacterCommand
                    {
                        MaxHealth = character.MaxHealth,
                        Strength = character.Strength,
                        Dexterity = character.Dexterity,
                        Speed = character.Speed,
                        Intellect = character.Intellect,
                        Armor = character.Armor,
                        AmmoType = character.AmmoType,
                        AmmoCount = character.AmmoCount
                    }
                };

                onConsumed();

                while (true)
                {
                    operationToken.ThrowIfCancellationRequested();
                    UpdateCharacterInventoryDto result;

                    try
                    {
                        result = await persist(request, operationToken);
                    }
                    catch (OperationCanceledException) when (operationToken.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (Exception exception)
                    {
                        onUncertain(exception);

                        await Task.Delay(TimeSpan.FromSeconds(2), operationToken);

                        continue;
                    }

                    if (result.Status == UpdateCharacterInventoryStatusEnum.Applied)
                    {
                        return;
                    }

                    var failure = new InvalidOperationException($"Ammo receipt returned {result.Status}.");
                    onUncertain(failure);

                    if (result.Status == UpdateCharacterInventoryStatusEnum.OperationRejected)
                    {
                        throw failure;
                    }

                    await Task.Delay(TimeSpan.FromSeconds(2), operationToken);
                }
            }
            finally
            {
                equipmentGate.Release();
            }
        }
    }
}
