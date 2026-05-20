using Assets.Scripts.Areas.Shared.Mono;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Pool;

namespace Assets.Scripts.Areas.Shared.UI
{
    public class LogUI : MonoSingleton<LogUI>
    {
        #region Prefab

        [SerializeField] private GameObject _textPrefab;

        #endregion

        #region GameObject

        public GameObject Canvas { get; private set; }

        public GameObject Log { get; private set; }

        public GameObject LogContent { get; private set; }

        #endregion

        private ObjectPool<LogPoolObject> _textPool;

        private void Start()
        {
            Canvas = GameObject.Find("LogCanvas");
            Log = Canvas.transform.Find("Log").gameObject;
            LogContent = Log.transform.Find("Viewport/Content").gameObject;

            _textPool = new ObjectPool<LogPoolObject>(
                createFunc: () =>
                {
                    var obj = Instantiate(_textPrefab);
                    var mesh = obj.GetComponent<TextMeshProUGUI>();

                    mesh.fontSize = 36;
                    mesh.alignment = TextAlignmentOptions.Center;

                    return new LogPoolObject
                    {
                        GameObject = obj,
                        Mesh = mesh
                    };
                },
                actionOnGet: obj =>
                {
                    obj.GameObject.transform.SetParent(LogContent.transform);
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

            ShowAsync("Hello, World!", 5000).Forget();
        }

        public async UniTask ShowAsync(string message, int delay = 2000)
        {
            if (!Log.activeSelf)
            {
                Log.SetActive(true);
            }

            var obj = _textPool.Get();
            obj.Mesh.text = message;

            await UniTask.Delay(delay);

            _textPool.Release(obj);

            if (LogContent.transform.childCount == 0)
            {
                Log.SetActive(false);
            }
        }

        private class LogPoolObject
        {
            public GameObject GameObject { get; set; }

            public TextMeshProUGUI Mesh { get; set; }
        }
    }
}
