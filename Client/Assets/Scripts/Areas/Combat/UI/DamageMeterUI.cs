using System.Collections.Generic;
using Assets.Scripts.Areas.Inventory.UI;
using Assets.Scripts.Areas.Shared.Enums;
using Assets.Scripts.Areas.Shared.Mono;
using Assets.Scripts.Areas.Shared.UI;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Assets.Scripts.Areas.Combat.UI
{
    public class DamageMeterUI : MonoBehaviour
    {
        [SerializeField] private GameObject _rowPrefab;
        private GameObject _panel;
        private RectTransform _content;
        private ScrollRect _scroll;
        private TMP_Text _empty;
        private readonly List<GameObject> _rows = new List<GameObject>();
        private RectTransform _inventory;
        private readonly Vector3[] _corners = new Vector3[4];

        private void Start()
        {
            _panel = transform.Find("DamageMeter").gameObject;
            foreach (var root in gameObject.scene.GetRootGameObjects())
            {
                if (root.name == "InventoryCanvas")
                {
                    _inventory = root.transform.Find("Inventory") as RectTransform;
                    break;
                }
            }

            if (_inventory == null)
            {
                Debug.LogWarning("[DamageMeterUI] Inventory panel was not found; docking is unavailable.", this);
            }

            _scroll = _panel.transform.Find("List").GetComponent<ScrollRect>();
            _content = (RectTransform)_panel.transform.Find("List/Viewport/Content");
            _empty = _panel.transform.Find("Empty").GetComponent<TMP_Text>();
            _panel.transform.Find("Close").GetComponent<Button>().onClick.AddListener(Hide);
            _panel.transform.Find("Reset").GetComponent<Button>().onClick.AddListener(ResetMeter);
            DamageMeterState.Changed += Refresh;
            _panel.SetActive(false);
        }

        private void Update()
        {
            var keyboard = Keyboard.current;

            if (keyboard != null && !InputFocusUI.IsAnyInputFocused && keyboard.rightBracketKey.wasPressedThisFrame)
            {
                Toggle();
            }
        }

        private void LateUpdate()
        {
            if (_panel != null && _panel.activeSelf)
            {
                UpdateLayout();
            }
        }

        public void Toggle()
        {
            if (_panel.activeSelf)
            {
                Hide();
            }
            else
            {
                _panel.SetActive(true);
                Debug.Log("[DamageMeterUI] Meter opened.", this);
                UpdateLayout();
                Refresh();
                ScrollToTop();
            }
        }

        public void Hide()
        {
            _panel.SetActive(false);
            Debug.Log("[DamageMeterUI] Meter closed.", this);
        }

        private void ResetMeter()
        {
            DamageMeterState.Reset();
            ScrollToTop();
            Debug.Log("[DamageMeterUI] Local meter reset.", this);
        }

        public void Refresh()
        {
            if (_panel == null || !_panel.activeSelf)
            {
                return;
            }

            var entries = DamageMeterState.Entries;

            while (_rows.Count < entries.Count)
            {
                _rows.Add(Instantiate(_rowPrefab, _content));
            }

            for (var i = 0; i < _rows.Count; i++)
            {
                _rows[i].SetActive(i < entries.Count);
            }

            _empty.text = TranslateManager.Instance.GetByKey(TranslateKeyEnum.DamageMeterEmpty);
            _empty.gameObject.SetActive(entries.Count == 0);
            _panel.transform.Find("Title").GetComponent<TMP_Text>().text = TranslateManager.Instance.GetByKey(TranslateKeyEnum.DamageMeterTitle);
            _panel.transform.Find("Reset/Text").GetComponent<TMP_Text>().text = TranslateManager.Instance.GetByKey(TranslateKeyEnum.DamageMeterReset);

            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                var row = _rows[i];
                var slot = row.transform.Find("Slot");
                var image = slot.Find("Background").GetComponent<RawImage>();
                InventoryUI.Instance.Textures.TryGetValue(entry.WeaponType, out var texture);
                image.texture = texture;
                image.color = Color.white;
                row.transform.Find("Name").GetComponent<TMP_Text>().text = entry.Nickname;
                var fraction = DamageMeterState.TotalDamage > 0 ? entry.Damage / DamageMeterState.TotalDamage : 0;
                row.transform.Find("Value").GetComponent<TMP_Text>().text = $"{entry.Damage:N0} ({fraction:P0})";
                var fill = (RectTransform)row.transform.Find("Bar/Fill");
                fill.anchorMax = new Vector2(Mathf.Clamp01((float)fraction), 1);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
        }

        private void ScrollToTop()
        {
            Canvas.ForceUpdateCanvases();
            _scroll.StopMovement();
            _scroll.verticalNormalizedPosition = 1f;
        }

        private void UpdateLayout()
        {
            if (_inventory == null)
            {
                return;
            }

            var canvasRect = (RectTransform)transform;
            var canvas = GetComponent<Canvas>();
            var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            var inventoryCanvas = _inventory.GetComponentInParent<Canvas>();
            var inventoryCamera = inventoryCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : inventoryCanvas.worldCamera;
            _inventory.GetWorldCorners(_corners);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect,
                RectTransformUtility.WorldToScreenPoint(inventoryCamera, _corners[0]), camera, out var min);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect,
                RectTransformUtility.WorldToScreenPoint(inventoryCamera, _corners[2]), camera, out var max);

            const float margin = 16f;
            const float gap = 10f;
            var bounds = canvasRect.rect;
            var availableWidth = Mathf.Max(0, min.x - gap - (bounds.xMin + margin));
            var desiredWidth = max.x - min.x;
            var desiredHeight = max.y - min.y;
            var panel = (RectTransform)_panel.transform;
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(1, 0);

            if (availableWidth >= Mathf.Min(200f, desiredWidth))
            {
                var width = Mathf.Min(desiredWidth, availableWidth);
                panel.anchoredPosition = new Vector2(min.x - gap - bounds.xMax, min.y - bounds.yMin);
                panel.sizeDelta = new Vector2(width, desiredHeight);
            }
            else
            {
                // Portrait screens cannot fit both panels side by side; stack them instead.
                var width = Mathf.Min(desiredWidth, bounds.width - 2 * margin);
                var height = Mathf.Min(desiredHeight, Mathf.Max(0, bounds.yMax - margin - max.y - gap));
                var right = Mathf.Min(max.x, bounds.xMax - margin);
                panel.anchoredPosition = new Vector2(right - bounds.xMax, max.y + gap - bounds.yMin);
                panel.sizeDelta = new Vector2(width, height);
            }
        }

        private void OnDestroy()
        {
            DamageMeterState.Changed -= Refresh;
        }
    }
}
