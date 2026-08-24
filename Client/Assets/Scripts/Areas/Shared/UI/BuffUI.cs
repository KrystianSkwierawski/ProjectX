using System;
using System.Collections.Generic;
using Assets.Scripts.Areas.Inventory.Enums;
using Assets.Scripts.Areas.Inventory.Models;
using Assets.Scripts.Areas.Inventory.UI;
using Assets.Scripts.Areas.Shared.Mono;
using Assets.Scripts.Areas.Shared.Subscriptions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Areas.Shared.UI
{
    public sealed class BuffUI : MonoSingleton<BuffUI>
    {
        [SerializeField] private Transform _bar;

        private readonly IDictionary<InventoryItemEnum, BuffSlot> _slots = new Dictionary<InventoryItemEnum, BuffSlot>();
        private readonly IList<string> _keys = new List<string>();

        protected override bool PersistBetweenScenes => false;

        protected override void Awake()
        {
            base.Awake();

            _bar ??= transform.Find("BuffBar");
        }

        private void Start()
        {
            ConfigureSlot(InventoryItemEnum.StrengthPotion);
            ConfigureSlot(InventoryItemEnum.SpeedPotion);
        }

        public void ShowOrRefresh(InventoryItemEnum type, float remainingSeconds)
        {
            if (!_slots.TryGetValue(type, out var slot))
            {
                return;
            }

            slot.GameObject.SetActive(true);
            SetRemaining(type, remainingSeconds);
        }

        public void SetRemaining(InventoryItemEnum type, float remainingSeconds)
        {
            if (!_slots.TryGetValue(type, out var slot))
            {
                return;
            }

            var totalSeconds = Mathf.Max(0, Mathf.CeilToInt(remainingSeconds));
            var time = TimeSpan.FromSeconds(totalSeconds);

            slot.Timer.text = time.TotalMinutes >= 1d
                ? $"{(int)time.TotalMinutes}:{time.Seconds:00}"
                : $"{time.Seconds}s";
        }

        public void Hide(InventoryItemEnum type)
        {
            if (_slots.TryGetValue(type, out var slot))
            {
                slot.GameObject.SetActive(false);
            }
        }

        private void ConfigureSlot(InventoryItemEnum type)
        {
            var root = _bar?.Find(type.ToString());

            if (root == null)
            {
                Debug.LogWarning($"BuffUI -> Missing slot. Type: {type}");

                return;
            }

            var image = root.Find("Background").GetComponent<RawImage>();
            image.texture = InventoryUI.Instance.Textures[type];
            image.color = ColorUI.White;

            var timer = root.Find("Text").GetComponent<TextMeshProUGUI>();
            var timerTransform = timer.rectTransform;
            timerTransform.anchorMin = Vector2.zero;
            timerTransform.anchorMax = Vector2.one;
            timerTransform.offsetMin = new Vector2(3f, 2f);
            timerTransform.offsetMax = new Vector2(-3f, -2f);

            timer.alignment = TextAlignmentOptions.BottomRight;
            timer.fontSize = 16f;
            timer.raycastTarget = false;
            timer.gameObject.SetActive(true);

            var preview = root.Find("Preview").gameObject;
            var previewTransform = preview.GetComponent<RectTransform>();
            previewTransform.anchorMin = new Vector2(1f, 0f);
            previewTransform.anchorMax = new Vector2(1f, 0f);
            previewTransform.pivot = new Vector2(1f, 1f);
            previewTransform.anchoredPosition = new Vector2(0f, -4f);

            var previewCanvas = preview.GetComponent<Canvas>();

            previewCanvas.sortingOrder = 11;

            var title = preview.transform.Find("Title").GetComponent<TextMeshProUGUI>();
            var description = preview.transform.Find("Description").GetComponent<TextMeshProUGUI>();
            var item = new InventoryItemDto
            {
                Type = type,
                Count = 1
            };

            title.text = TranslateManager.Instance.GetByKey($"{type}Title");
            description.text = InventoryUI.Instance.PrepareDescription(item);

            preview.SetActive(false);

            var hover = root.GetComponent<HoverUI>();
            var button = root.GetComponent<ButtonUI>();

            hover.enabled = true;

            button.interactable = true;
            button.onClick.RemoveAllListeners();

            var key = root.gameObject.GetInstanceID().ToString();
            _keys.Add(key);

            OnPointerEnterSubscription.Instance.Subscribe(key, (e) =>
            {
                preview.SetActive(true);
            });

            OnPointerExitSubscription.Instance.Subscribe(key, (e) =>
            {
                preview.SetActive(false);
            });

            root.gameObject.SetActive(false);

            _slots.Add(type, new BuffSlot
            {
                GameObject = root.gameObject,
                Timer = timer
            });
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

        private sealed class BuffSlot
        {
            public GameObject GameObject { get; set; }

            public TextMeshProUGUI Timer { get; set; }
        }
    }
}
