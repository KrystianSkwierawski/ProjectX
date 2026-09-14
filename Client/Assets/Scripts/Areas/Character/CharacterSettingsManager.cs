using System;
using System.Linq;
using System.Threading;
using Assets.Scripts.Areas.Character.Models;
using Assets.Scripts.Areas.Inventory.Enums;
using Assets.Scripts.Areas.Shared.Mono;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Areas.Character
{
    public class CharacterSettingsManager : Singleton<CharacterSettingsManager>
    {
        public const int SlotCount = 10;
        private readonly SemaphoreSlim _saveGate = new SemaphoreSlim(1, 1);

        public CharacterSettingsDto Dto { get; private set; }

        public async UniTask LoadAsync(int characterId)
        {
            Dto = null;
            var dto = await UnityWebRequestHelper.ExecuteGetAsync<CharacterSettingsDto>(
                $"CharacterSettings?CharacterId={characterId}", log: false);

            if (dto?.CharacterId != characterId || dto.ActionBars?.Length != SlotCount)
            {
                throw new InvalidOperationException("Invalid character settings response.");
            }

            Dto = dto;
            UserManager.Instance.SetCharacterLanguage(dto.Language);
            Debug.Log($"Character settings loaded. CharacterId: {characterId}");
        }

        public async UniTask SaveBindingsAsync(InventoryItemEnum[] actionBars, CancellationToken cancellationToken)
        {
            var characterId = UserManager.Instance.SelectedCharacterId;
            var bindings = actionBars.ToArray();
            await _saveGate.WaitAsync(cancellationToken);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (Dto?.CharacterId != characterId || UserManager.Instance.SelectedCharacterId != characterId)
                {
                    return;
                }

                var result = await UnityWebRequestHelper.ExecutePostAsync<CharacterSettingsDto>("CharacterSettings",
                    new CharacterSettingsDto { CharacterId = characterId, Language = Dto.Language, ActionBars = bindings },
                    log: false, cancellationToken: cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();

                if (UserManager.Instance.SelectedCharacterId == characterId && result?.CharacterId == characterId)
                {
                    Dto = result;
                    Debug.Log($"Action bar bindings saved. CharacterId: {characterId}");
                }
            }
            finally
            {
                _saveGate.Release();
            }
        }
    }
}
