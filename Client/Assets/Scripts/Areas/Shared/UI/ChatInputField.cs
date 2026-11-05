using Assets.Scripts.Areas.Inventory.Enums;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Assets.Scripts.Areas.Shared.UI
{
    public sealed class ChatInputField : TMP_InputField
    {
        private PointerEventData _linkPointer;
        private int _linkFrame = -1;
        private int _linkAnchor;
        private int _linkFocus;
        private bool _restoreFocusSelectAll;

        public override void OnDeselect(BaseEventData eventData)
        {
            HandleDeselect(eventData, Keyboard.current?.ctrlKey.isPressed == true);
        }

        private void HandleDeselect(BaseEventData eventData, bool controlPressed)
        {
            _linkPointer = null;

            // The UI input module deselects before dispatching the same pointer-down to the slot.
            if (isFocused && eventData is PointerEventData pointer
                && pointer.button == PointerEventData.InputButton.Left && controlPressed)
            {
                _linkPointer = pointer;
                _linkFrame = Time.frameCount;
                _linkAnchor = selectionStringAnchorPosition;
                _linkFocus = selectionStringFocusPosition;
            }

            base.OnDeselect(eventData);
        }

        internal bool TryInsertItem(InventoryItemEnum item, PointerEventData pointer)
        {
            var refocus = !isFocused && _linkFrame == Time.frameCount && ReferenceEquals(_linkPointer, pointer);

            if ((!isFocused && !refocus) || !isActiveAndEnabled || !ChatItemLink.TryEncode(item, out var token))
            {
                return false;
            }

            _linkPointer = null;
            var draft = text;
            var anchor = refocus ? _linkAnchor : selectionStringAnchorPosition;
            var focus = refocus ? _linkFocus : selectionStringFocusPosition;
            var start = Mathf.Clamp(Mathf.Min(anchor, focus), 0, draft.Length);
            var end = Mathf.Clamp(Mathf.Max(anchor, focus), start, draft.Length);
            var updated = draft.Remove(start, end - start).Insert(start, token.ToString());

            if (HasRoomForLink(updated))
            {
                text = updated;
                selectionStringAnchorPosition = selectionStringFocusPosition = start + 1;
            }
            else
            {
                selectionStringAnchorPosition = anchor;
                selectionStringFocusPosition = focus;
                Debug.Log("[ChatInputField] Item link insertion skipped because the draft reached its length or item link limit.", this);
            }

            if (refocus)
            {
                _restoreFocusSelectAll = onFocusSelectAll;
                onFocusSelectAll = false;
                ActivateInputField();
            }

            return true;
        }

        private static bool HasRoomForLink(string draft)
        {
            // A whisper's recipient prefix does not count toward the transport limit.
            if (draft.Length > ChatItemLink.MaximumMessageLength
                && !draft.TrimStart().StartsWith("/w ", System.StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var linkCount = 0;
            foreach (var character in draft)
            {
                if (ChatItemLink.TryDecode(character, out _) && ++linkCount > ChatItemLink.MaximumItemLinks)
                {
                    return false;
                }
            }

            return true;
        }

        protected override void LateUpdate()
        {
            base.LateUpdate();

            if (_restoreFocusSelectAll)
            {
                onFocusSelectAll = true;
                _restoreFocusSelectAll = false;
            }
        }
    }
}
