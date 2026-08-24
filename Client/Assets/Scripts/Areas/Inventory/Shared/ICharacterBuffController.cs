using System;
using Assets.Scripts.Areas.Character.Models;
using Assets.Scripts.Areas.Inventory.Enums;

namespace Assets.Scripts.Areas.Inventory.Shared
{
    public interface ICharacterBuffController
    {
        void ApplyOrRefreshBuff(
            InventoryItemEnum type,
            float durationSeconds,
            Action<CharacterDto, bool> setActive);
    }
}
