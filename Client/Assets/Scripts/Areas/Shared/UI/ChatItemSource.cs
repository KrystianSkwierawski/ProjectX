using Assets.Scripts.Areas.Inventory.Enums;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Assets.Scripts.Areas.Shared.UI
{
    public sealed class ChatItemSource : MonoBehaviour, IPointerDownHandler, IPointerClickHandler
    {
        private InventoryItemEnum _item;
        private int? _consumedPointer;

        public static void Bind(Transform source, InventoryItemEnum item)
        {
            if (source.TryGetComponent<ButtonUI>(out var button))
            {
                button.ChatItem = item;

                return;
            }

            if (!source.TryGetComponent<ChatItemSource>(out var link))
            {
                link = source.gameObject.AddComponent<ChatItemSource>();
            }

            link._item = item;
        }

        internal static bool TryInsert(InventoryItemEnum item, PointerEventData eventData)
        {
            return Keyboard.current?.ctrlKey.isPressed == true && ChatUI.Instance?.TryInsertItem(item, eventData) == true;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _consumedPointer = null;

            if (eventData.button == PointerEventData.InputButton.Left && TryInsert(_item, eventData))
            {
                _consumedPointer = eventData.pointerId;
                eventData.Use();
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left && _consumedPointer == eventData.pointerId)
            {
                eventData.Use();
            }
        }
    }
}
