using System.Collections.Generic;
using Assets.Scripts.Areas.Inventory.Enums;
using Assets.Scripts.Areas.Inventory.UI;
using Assets.Scripts.Areas.Shared.Mono;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Pool;
using UnityEngine.UI;

namespace Assets.Scripts.Areas.Shared.UI
{
    public class ChatUI : MonoSingleton<ChatUI>
    {
        #region Prefab

        [SerializeField] private GameObject _textPrefab;

        #endregion

        #region GameObject

        public GameObject Canvas { get; private set; }

        public GameObject Container { get; private set; }

        public ChatInputField InputField { get; private set; }

        public GameObject Chat { get; private set; }

        public GameObject ChatContent { get; private set; }

        private ScrollRect _scrollRect;
        private RectTransform _chatContentRect;
        private RectTransform _inventory;
        private RectTransform _damageMeter;
        private RectTransform _playerStatus;
        private RectTransform[] _otherPanels;
        private readonly Vector3[] _panelCorners = new Vector3[4];

        #endregion

        private ObjectPool<LogPoolObject> _pool;
        private Queue<LogPoolObject> _queue = new Queue<LogPoolObject>();


        private void Start()
        {
            Canvas = GameObject.Find("ChatCanvas");
            Container = Canvas.transform.Find("Container").gameObject;
            Container.transform.Find("Close").GetComponent<Button>().onClick.AddListener(Toggle);
            InputField = Container.transform.Find("InputField").GetComponent<ChatInputField>();
            BindItemText((TextMeshProUGUI)InputField.textComponent, linkSource: false);
            Chat = Container.transform.Find("Chat").gameObject;
            ChatContent = Chat.transform.Find("Viewport/Content").gameObject;
            _scrollRect = Chat.GetComponent<ScrollRect>() ?? Chat.GetComponentInChildren<ScrollRect>();
            _chatContentRect = ChatContent.GetComponent<RectTransform>();
            foreach (var root in gameObject.scene.GetRootGameObjects())
            {
                if (root.name == "InventoryCanvas")
                {
                    _inventory = root.transform.Find("Inventory") as RectTransform;
                }
                else if (root.name == "QuestCanvas")
                {
                    _damageMeter = root.transform.Find("DamageMeter") as RectTransform;
                }
                else if (root.name == "PlayerCanvas")
                {
                    _playerStatus = root.transform.Find("Player") as RectTransform;
                }
            }

            if (_inventory == null)
            {
                Debug.LogWarning("[ChatUI] Inventory panel was not found; matching chat alignment is unavailable.", this);
            }

            _otherPanels = new[] { _inventory, _damageMeter };

            _pool = new ObjectPool<LogPoolObject>(
                createFunc: () =>
                {
                    var obj = Instantiate(_textPrefab);
                    var mesh = obj.GetComponent<TextMeshProUGUI>();

                    mesh.fontSize = 16;
                    BindItemText(mesh, linkSource: true);
                    mesh.alignment = TextAlignmentOptions.TopLeft;

                    return new LogPoolObject
                    {
                        GameObject = obj,
                        Mesh = mesh
                    };
                },
                actionOnGet: obj =>
                {
                    obj.GameObject.transform.SetParent(ChatContent.transform, false);
                    obj.GameObject.SetActive(true);
                    obj.GameObject.transform.SetAsLastSibling();
                },
                actionOnRelease: obj =>
                {
                    obj.GameObject.transform.SetParent(null);
                    obj.GameObject.SetActive(false);
                    obj.Mesh.text = string.Empty;
                }
            );

            Add("Welcome to the chat!");
        }

        private static void BindItemText(TextMeshProUGUI mesh, bool linkSource)
        {
            var inventory = InventoryUI.Instance;

            ChatItemText.Bind(mesh, inventory.SlotPrefab, inventory.Textures, linkSource);
        }

        private void LateUpdate()
        {
            if (Container == null || _inventory == null)
            {
                return;
            }

            var canvasRect = (RectTransform)Canvas.transform;
            var canvas = Canvas.GetComponent<Canvas>();
            var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            GetPanelBounds(_inventory, canvasRect, camera, out var inventoryMin, out var inventoryMax);

            const float margin = 16f;
            const float gap = 10f;
            var bounds = canvasRect.rect;
            var left = bounds.xMin + margin;
            var width = Mathf.Min(416f, Mathf.Max(0, bounds.width - 2 * margin));
            var bottom = inventoryMin.y;
            var height = inventoryMax.y - inventoryMin.y;
            var topOfPanels = bottom;
            var overlaps = false;

            foreach (var panel in _otherPanels)
            {
                if (panel == null || !panel.gameObject.activeInHierarchy)
                {
                    continue;
                }

                GetPanelBounds(panel, canvasRect, camera, out var min, out var max);
                if (min.x < left + width + gap && max.x > left && max.y > bottom)
                {
                    topOfPanels = Mathf.Max(topOfPanels, max.y);
                    overlaps |= min.y < bottom + height + gap;
                }
            }

            if (overlaps)
            {
                bottom = topOfPanels + gap;
                var top = bounds.yMax - margin;
                if (_playerStatus != null && _playerStatus.gameObject.activeInHierarchy)
                {
                    GetPanelBounds(_playerStatus, canvasRect, camera, out var statusMin, out var statusMax);
                    if (statusMin.x < left + width && statusMax.x > left)
                    {
                        var spaceBelowStatus = statusMin.y - gap - bottom;
                        var spaceRightOfStatus = bounds.xMax - margin - statusMax.x - gap;
                        if (spaceBelowStatus < 160 && spaceRightOfStatus >= 200)
                        {
                            left = statusMax.x + gap;
                            width = Mathf.Min(width, spaceRightOfStatus);
                        }
                        else
                        {
                            top = Mathf.Min(top, statusMin.y - gap);
                        }
                    }
                }

                height = Mathf.Min(height, Mathf.Max(0, top - bottom));
            }

            var rect = (RectTransform)Container.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
            rect.anchoredPosition = new Vector2(left - bounds.xMin, bottom - bounds.yMin);
            rect.sizeDelta = new Vector2(width, height);
        }

        private void GetPanelBounds(RectTransform panel, RectTransform canvasRect, Camera camera, out Vector2 min, out Vector2 max)
        {
            var panelCanvas = panel.GetComponentInParent<Canvas>();
            var panelCamera = panelCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : panelCanvas.worldCamera;
            panel.GetWorldCorners(_panelCorners);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect,
                RectTransformUtility.WorldToScreenPoint(panelCamera, _panelCorners[0]), camera, out min);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect,
                RectTransformUtility.WorldToScreenPoint(panelCamera, _panelCorners[2]), camera, out max);
        }

        public void Add(string message, string sender = "SYSTEM")
        {
            Add(message, sender, ColorUI.TextPrimary, string.Empty);
        }

        public void AddWhisper(string message, string characterName, bool outgoing)
        {
            var prefix = outgoing ? "→ " : "← ";

            Add(message, characterName, ColorUI.AccentHover, prefix);
        }

        public void BeginWhisper(string characterName)
        {
            Container.SetActive(true);
            var escapedCharacterName = (characterName ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");
            InputField.text = $"/w \"{escapedCharacterName}\" ";
            InputField.ActivateInputField();
            InputField.MoveTextEnd(false);
        }

        private void Add(string message, string sender, Color color, string prefix)
        {
            var obj = _pool.Get();

            obj.Mesh.text = $"{prefix}{SanitizeSender(sender)}: {Sanitize(message)}";
            obj.Mesh.color = color;

            _queue.Enqueue(obj);

            CheckRetention();

            if (!InputField.isFocused)
            {
                ScrollToBottom();
            }
        }

        internal bool TryInsertItem(InventoryItemEnum item, PointerEventData pointer) => InputField?.TryInsertItem(item, pointer) == true;

        private static string SanitizeSender(string value)
        {
            var characters = Sanitize(value).ToCharArray();

            for (var index = 0; index < characters.Length; index++)
            {
                if (ChatItemLink.IsReserved(characters[index]))
                {
                    characters[index] = '\uFFFD';
                }
            }

            return new string(characters);
        }

        private static string Sanitize(string value)
        {
            return (value ?? string.Empty).Replace("<", "‹").Replace(">", "›");
        }

        public void ScrollToBottom()
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(_chatContentRect);
            _scrollRect.verticalNormalizedPosition = 0f;
        }

        public void Toggle()
        {
            Container.SetActive(!Container.activeSelf);
        }

        private void CheckRetention()
        {
            if (_queue.Count > 100)
            {
                var obj = _queue.Dequeue();

                _pool.Release(obj);
            }
        }

        private class LogPoolObject
        {
            public GameObject GameObject { get; set; }

            public TextMeshProUGUI Mesh { get; set; }
        }
    }
}
