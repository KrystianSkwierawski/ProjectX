using System;
using Assets.Scripts.Areas.Inventory.Enums;

namespace Assets.Scripts.Areas.Shared.UI
{
    // One UTF-16 code unit per item. IDs are stable across client and server builds.
    public static class ChatItemLink
    {
        public const int MaximumMessageLength = 200;
        public const int MaximumItemLinks = 8;
        private const int TokenStart = 0xE000;
        private const int TokenEnd = 0xF8FF;

        public static bool IsReserved(char value) => value >= TokenStart && value <= TokenEnd;

        public static bool TryEncode(InventoryItemEnum item, out char token)
        {
            var id = (int)item;

            if (id <= 0 || id > TokenEnd - TokenStart || !IsLinkable(item))
            {
                token = default;

                return false;
            }

            token = (char)(TokenStart + id);

            return true;
        }

        public static bool TryDecode(char token, out InventoryItemEnum item)
        {
            item = IsReserved(token) ? (InventoryItemEnum)(token - TokenStart) : InventoryItemEnum.None;

            return IsLinkable(item);
        }

        public static bool IsValidMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message) || message.Length > MaximumMessageLength)
            {
                return false;
            }

            var itemLinks = 0;

            foreach (var character in message)
            {
                if (IsReserved(character) && (!TryDecode(character, out _) || ++itemLinks > MaximumItemLinks))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsLinkable(InventoryItemEnum item) =>
            item is not (InventoryItemEnum.None or InventoryItemEnum.HelmetTemplate or InventoryItemEnum.ChestTemplate
                or InventoryItemEnum.BootsTemplate or InventoryItemEnum.WeaponTemplate or InventoryItemEnum.AmmoTemplate)
            && Enum.IsDefined(typeof(InventoryItemEnum), item);
    }
}
