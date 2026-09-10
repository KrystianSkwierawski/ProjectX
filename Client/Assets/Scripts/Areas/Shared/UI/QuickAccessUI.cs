using System.Collections.Generic;
using Assets.Scripts.Areas.Character.UI;
using Assets.Scripts.Areas.Friends.UI;
using Assets.Scripts.Areas.Inventory.UI;
using Assets.Scripts.Areas.Shared.Mono;
using Assets.Scripts.Areas.Shared.Subscriptions;
using UnityEngine;
using UnityEngine.Events;

namespace Assets.Scripts.Areas.Shared.UI
{
    public class QuickAccessUI : MonoSingleton<QuickAccessUI>
    {
        private readonly IList<string> _keys = new List<string>();
        private readonly IList<GameObject> _previews = new List<GameObject>();
        private Transform _bar;
        private bool _previewsEnabled = true;

        protected override bool PersistBetweenScenes => false;

        protected override void Awake()
        {
            base.Awake();

            _bar = transform.Find("QuickAccessBar");
        }

        private void Start()
        {
            ConfigureSlot("Gear", () => GearUI.Instance.Toggle());

            ConfigureSlot("Inventory", () => InventoryUI.Instance.Toggle());

            ConfigureSlot("Character", () => CharacterUI.Instance.Toggle());

            ConfigureSlot("Chat", () => ChatUI.Instance.Toggle());

            ConfigureSlot("Friends", () => FriendListUI.Instance.Toggle());
        }

        private void ConfigureSlot(string name, UnityAction onClick)
        {
            var slot = _bar.Find(name);

            if (slot == null)
            {
                Debug.LogWarning($"QuickAccessUI -> Missing slot. Name: {name}");

                return;
            }

            var preview = slot.Find("Preview").gameObject;
            _previews.Add(preview);

            var button = slot.GetComponent<ButtonUI>();
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(onClick);

            var key = slot.gameObject.GetInstanceID().ToString();
            _keys.Add(key);

            OnPointerEnterSubscription.Instance.Subscribe(key, (e) =>
            {
                if (_previewsEnabled)
                {
                    preview.SetActive(true);
                }
            });

            OnPointerExitSubscription.Instance.Subscribe(key, (e) =>
            {
                preview.SetActive(false);
            });
        }

        public void SetPreviewsEnabled(bool enabled)
        {
            _previewsEnabled = enabled;

            if (enabled)
            {
                return;
            }

            foreach (var preview in _previews)
            {
                preview.SetActive(false);
            }
        }

        protected override void OnDestroy()
        {
            foreach (var key in _keys)
            {
                OnPointerEnterSubscription.Instance.Unsubscribe(key);
                OnPointerExitSubscription.Instance.Unsubscribe(key);
            }

            base.OnDestroy();
        }
    }
}
