using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Assets.Scripts.Areas.Shared.Subscriptions;

namespace Assets.Scripts.Areas.Shared.UI
{
    public class HoverUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private string _key;

        public void Start()
        {
            _key = gameObject.GetInstanceID().ToString();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (gameObject.TryGetComponent<Button>(out var button) && !button.interactable)
            {
                return;
            }

            CursorUI.Instance.ShowPointer();
            OnPointerEnterSubscription.Instance.Invoke(_key, new OnPointerEnterEvent());
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            CursorUI.Instance.ShowDefault();
            OnPointerExitSubscription.Instance.Invoke(_key, new OnPointerExitSubscriptionEvent());
        }

        public void OnDisable()
        {
            CursorUI.Instance.ShowDefault();
            OnPointerExitSubscription.Instance.Invoke(_key, new OnPointerExitSubscriptionEvent());
        }
    }
}
