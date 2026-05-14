using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Assets.Scripts.UI
{
    public class ButtonUI : UnityEngine.UI.Button
    {
        public UnityEvent OnRightClick { get; private set; } = new UnityEvent();

        public override void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                OnRightClick?.Invoke();

                return;
            }

            base.OnPointerDown(eventData);
        }
    }
}
