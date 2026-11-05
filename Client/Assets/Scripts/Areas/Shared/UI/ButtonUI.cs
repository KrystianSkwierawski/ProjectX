using Assets.Scripts.Areas.Inventory.Enums;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Assets.Scripts.Areas.Shared.UI
{
    public class ButtonUI : UnityEngine.UI.Button
    {
        public UnityEvent OnRightClick { get; private set; } = new UnityEvent();

        internal InventoryItemEnum ChatItem { get; set; }

        private int? _consumedPointer;

        internal bool IsConsumedPointer(PointerEventData eventData) => _consumedPointer == eventData.pointerId;

        public override void OnPointerDown(PointerEventData eventData)
        {
            _consumedPointer = null;

            if (eventData.button == PointerEventData.InputButton.Left && ChatItemSource.TryInsert(ChatItem, eventData))
            {
                _consumedPointer = eventData.pointerId;
                eventData.Use();

                return;
            }

            if (eventData.button == PointerEventData.InputButton.Right)
            {
                OnRightClick?.Invoke();

                return;
            }

            base.OnPointerDown(eventData);
        }

        public override void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left && IsConsumedPointer(eventData))
            {
                eventData.Use();

                return;
            }

            base.OnPointerClick(eventData);
        }
    }
}
