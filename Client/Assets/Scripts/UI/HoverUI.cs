using UnityEngine;
using UnityEngine.EventSystems;

namespace Assets.Scripts.UI
{
    public class HoverUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public void OnPointerEnter(PointerEventData eventData)
        {
            CursorUI.Instance.ShowPointer();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            CursorUI.Instance.ShowDefault();
        }

        public void OnDisable()
        {
            CursorUI.Instance.ShowDefault();
        }
    }
}
