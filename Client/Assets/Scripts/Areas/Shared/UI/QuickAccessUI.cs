using System.Collections.Generic;
using Assets.Scripts.Areas.Character.UI;
using Assets.Scripts.Areas.Inventory.Enums;
using Assets.Scripts.Areas.Inventory.UI;
using Assets.Scripts.Areas.Shared.Subscriptions;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Assets.Scripts.Areas.Shared.UI
{
    public class QuickAccessUI : MonoBehaviour
    {
        [SerializeField] private Transform _bar;

        private readonly IList<string> _keys = new List<string>();

        private void Start()
        {
            _bar ??= transform.Find("QuickAccessBar");

            ConfigureSlot("Gear", "Gear (TAB)", () => GearUI.Instance.Toggle());
            ConfigureSlot("Inventory", "Inventory (B)", () => InventoryUI.Instance.Toggle());
            ConfigureSlot("Character", "Character (C)", () => CharacterUI.Instance.Toggle());
            ConfigureSlot("Chat", "Chat (Z)", () => ChatUI.Instance.Toggle());
        }

        private void ConfigureSlot(string name, string tooltip, UnityAction onClick)
        {
            var slot = _bar.Find(name);

            if (slot == null)
            {
                Debug.LogWarning($"QuickAccessUI -> Missing slot. Name: {name}");

                return;
            }

            slot.Find("Text").gameObject.SetActive(false);

            var image = slot.Find("Background").GetComponent<RawImage>();
            image.color = ColorUI.White;
            image.texture = Resources.Load<Texture>($"Icons/QuickAccess{name}") ?? Resources.Load<Texture>($"Icons/{InventoryItemEnum.None}");

            var preview = slot.Find("Preview").gameObject;
            preview.transform.Find("Title").GetComponent<TextMeshProUGUI>().text = tooltip;
            preview.transform.Find("Description").gameObject.SetActive(false);
            preview.SetActive(false);

            var button = slot.GetComponent<ButtonUI>();
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(onClick);

            var key = slot.gameObject.GetInstanceID().ToString();
            _keys.Add(key);

            OnPointerEnterSubscription.Instance.Subscribe(key, (e) =>
            {
                preview.SetActive(true);
            });

            OnPointerExitSubscription.Instance.Subscribe(key, (e) =>
            {
                preview.SetActive(false);
            });
        }

        private void OnDestroy()
        {
            foreach (var key in _keys)
            {
                OnPointerEnterSubscription.Instance.Unsubscribe(key);
                OnPointerExitSubscription.Instance.Unsubscribe(key);
            }
        }
    }
}
