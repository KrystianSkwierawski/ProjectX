using Assets.Scripts.Enums;
using Assets.Scripts.Models;
using Assets.Scripts.Shared;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Assets.Scripts.Mono
{
    public class Inventory : MonoBehaviour
    {
        public CharacterInventoryDto CharacterInventory { get; set; }

        private async void Start()
        {
            await UniTask.WaitUntil(() => !string.IsNullOrEmpty(TokenManager.Instance.Token));

            CharacterInventory = await UnityWebRequestHelper.ExecuteGetAsync<CharacterInventoryDto>("CharacterInventories?CharacterId=1");
            UIManager.Instance.InitInventory(CharacterInventory.count);
        }

        private void Update()
        {
            if (Keyboard.current.tabKey.wasPressedThisFrame)
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
            }
            else
            {
                AudioManager.Instance.PlayOneShot(AudioTypeEnum.InventoryOpen, 0.5f);

                UIManager.Instance.Inventory.SetActive(true);
            }
        }
    }
}