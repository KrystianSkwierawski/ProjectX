using Assets.Scripts.Areas.Shared.Mono;
using Assets.Scripts.Areas.Shared.Subscriptions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Areas.Shared.UI
{
    [RequireComponent(typeof(HoverUI))]
    public class WindowHelpUI : MonoBehaviour
    {
        [SerializeField] private string _translationKey;

        private string _key;
        private RectTransform _preview;
        private TextMeshProUGUI _title;
        private TextMeshProUGUI _description;

        private void Start()
        {
            _key = gameObject.GetInstanceID().ToString();
            _preview = (RectTransform)transform.Find("Preview");
            _title = _preview.Find("Title").GetComponent<TextMeshProUGUI>();
            _description = _preview.Find("Description").GetComponent<TextMeshProUGUI>();

            OnPointerEnterSubscription.Instance.Subscribe(_key, _ => Show());
            OnPointerExitSubscription.Instance.Subscribe(_key, _ => Hide());
            GetComponent<Button>().onClick.AddListener(() => GetComponent<HoverUI>().OnPointerEnter(null));
        }

        private void Show()
        {
            var text = TranslateManager.Instance.GetByKey(_translationKey);
            var separator = text.IndexOf('\n');

            _title.text = separator < 0 ? text : text.Substring(0, separator);
            _description.text = separator < 0 ? string.Empty : text.Substring(separator + 1).TrimStart();

            _preview.gameObject.SetActive(true);
        }

        private void Hide()
        {
            if (_preview != null)
            {
                _preview.gameObject.SetActive(false);
            }
        }

        private void OnDisable()
        {
            Hide();
        }

        private void OnDestroy()
        {
            if (_key != null)
            {
                OnPointerEnterSubscription.Instance.Unsubscribe(_key);
                OnPointerExitSubscription.Instance.Unsubscribe(_key);
            }
        }
    }
}
