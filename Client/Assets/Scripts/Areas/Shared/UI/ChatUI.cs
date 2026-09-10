using System.Collections.Generic;
using Assets.Scripts.Areas.Shared.Mono;
using TMPro;
using UnityEngine;
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

        public TMP_InputField InputField { get; private set; }

        public GameObject Chat { get; private set; }

        public GameObject ChatContent { get; private set; }

        private ScrollRect _scrollRect;
        private RectTransform _chatContentRect;

        #endregion

        private ObjectPool<LogPoolObject> _pool;
        private Queue<LogPoolObject> _queue = new Queue<LogPoolObject>();


        private void Start()
        {
            Canvas = GameObject.Find("ChatCanvas");
            Container = Canvas.transform.Find("Container").gameObject;
            InputField = Container.transform.Find("InputField").GetComponent<TMP_InputField>();
            Chat = Container.transform.Find("Chat").gameObject;
            ChatContent = Chat.transform.Find("Viewport/Content").gameObject;
            _scrollRect = Chat.GetComponent<ScrollRect>() ?? Chat.GetComponentInChildren<ScrollRect>();
            _chatContentRect = ChatContent.GetComponent<RectTransform>();

            _pool = new ObjectPool<LogPoolObject>(
                createFunc: () =>
                {
                    var obj = Instantiate(_textPrefab);
                    var mesh = obj.GetComponent<TextMeshProUGUI>();

                    mesh.fontSize = 14;
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

            obj.Mesh.text = $"{prefix}{Sanitize(sender)}: {Sanitize(message)}";
            obj.Mesh.color = color;

            _queue.Enqueue(obj);

            CheckRetention();

            if (!InputField.isFocused)
            {
                ScrollToBottom();
            }
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
