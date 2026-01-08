using System;
using System.Linq;
using System.Text;
using Assets.Scripts.Enums;
using Assets.Scripts.Models;
using Assets.Scripts.Shared;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Assets.Scripts.Mono
{
    public class CharacterInventory : NetworkBehaviour
    {
        public CharacterInventoryDto Inventory { get; set; }

        private async void Start()
        {
            await UniTask.WaitUntil(() => !string.IsNullOrEmpty(TokenManager.Instance.Token));

            if (IsOwner)
            {
                Inventory = await UnityWebRequestHelper.ExecuteGetAsync<CharacterInventoryDto>("CharacterInventories?CharacterId=1");
                UIManager.Instance.InitInventory(Inventory.count);
            }

            if (IsServer)
            {
                CombatManager.Instance.OnKillEvent.AddListener(async (KillEventModel killEvent) =>
                {
                    // FIXME: drop chance by enemy and inventory modo
                    int random = UnityEngine.Random.Range(0, 99);

                    if (random < 90)
                    {
                        var item = new InventoryItem
                        {
                            type = CharacterInventoryTypeEnum.Can,
                            count = 1
                        };

                        await UnityWebRequestHelper.ExecutePostAsync<EmptyResponse>("CharacterInventories", new AddCharacterInventoryItemCommand
                        {
                            characterId = 1,
                            inventoryItem = item
                        }, killEvent.ClientToken);

                        var progres = await QuestManager.Instance.CheckCharacterQuestProgresAsync(1, nameof(CharacterInventoryTypeEnum.Can), 1, killEvent.ClientToken);

                        UpdateInventoryClientRpc(item, killEvent.ClientId);

                        if (progres.status != CharacterQuestStatusEnum.None)
                        {
                            UpdateQuestLogClientRpc(progres.characterQuestId, 1, progres.status, killEvent.ClientId);
                        }
                    }
                });
            }
        }

        // FIXME!
        [ClientRpc]
        private void UpdateQuestLogClientRpc(int characterQuestId, int progres, CharacterQuestStatusEnum status, ulong clientId)
        {
            if (NetworkManager.Singleton.LocalClientId == clientId)
            {
                Debug.Log($"UpdateQuestLogClientRpc: {clientId}");

                var characterQuest = QuestManager.Instance.CharacterQuests
                    .Where(x => x.id == characterQuestId)
                    .Single();

                characterQuest.progress += progres;
                characterQuest.status = status;

                _ = UpdateQuestLogAsync();

                if (status == CharacterQuestStatusEnum.Finished)
                {
                    var npc = QuestManager.Instance.QuestNpcs[characterQuest.questId];

                    npc.HideExclamationMark();
                    npc.ShowQuestionMark();
                }
            }
        }

        private async UniTask UpdateQuestLogAsync()
        {
            await UniTask.WaitUntil(() => QuestManager.Instance.CharacterQuests != null);

            if (QuestManager.Instance.CharacterQuests.Any())
            {
                var sb = new StringBuilder();

                foreach (var characterQuest in QuestManager.Instance.CharacterQuests.Where(x => x.status is CharacterQuestStatusEnum.Accepted or CharacterQuestStatusEnum.Finished))
                {
                    var quest = QuestManager.Instance.Quests
                        .Where(x => x.id == characterQuest.questId)
                        .Single();

                    var log = string.Format(quest.statusText, Math.Min(characterQuest.progress, quest.requirement), quest.requirement);

                    sb.AppendLine(log);
                }

                UIManager.Instance.SetQuestLog(sb.ToString());
            }
        }

        private void Update()
        {
            if (IsOwner && Keyboard.current.tabKey.wasPressedThisFrame)
            {
                // TODO: wait?
                ToggleInventory();
            }
        }

        private static void ToggleInventory()
        {
            if (UIManager.Instance.Inventory.activeSelf)
            {
                AudioManager.Instance.PlayOneShot(AudioTypeEnum.InventoryClose, 0.5f);

                UIManager.Instance.Inventory.SetActive(false);

                return;
            }

            AudioManager.Instance.PlayOneShot(AudioTypeEnum.InventoryOpen, 0.5f);

            UIManager.Instance.Inventory.SetActive(true);
        }

        // FIXME!
        [ClientRpc]
        private void UpdateInventoryClientRpc(InventoryItem item, ulong clientId)
        {
            if (NetworkManager.Singleton.LocalClientId == clientId)
            {
                UIManager.Instance.AddInventoryItem(item);
            }
        }
    }
}