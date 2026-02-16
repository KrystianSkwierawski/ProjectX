using Assets.Scripts.Subscriptions;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Assets.Scripts.UI
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
