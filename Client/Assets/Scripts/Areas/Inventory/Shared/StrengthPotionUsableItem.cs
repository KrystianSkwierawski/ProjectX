using Assets.Scripts.Areas.Character.Models;
using Assets.Scripts.Areas.Inventory.Models;

namespace Assets.Scripts.Areas.Inventory.Shared
{
    public sealed class StrengthPotionUsableItem : AbstractBuffPotionUsableItem
    {
        public const short Bonus = 20;
        public const float Duration = 60f;

        public StrengthPotionUsableItem(
            InventoryItemDto item,
            string playerSessionId,
            ulong ownerClientId,
            ICharacterBuffController buffController)
            : base(item, playerSessionId, ownerClientId, buffController)
        {
        }

        protected override float DurationSeconds => Duration;

        protected override void SetBuff(CharacterDto character, bool active)
        {
            ApplyBuff(character, active);
        }

        public static void ApplyBuff(CharacterDto character, bool active)
        {
            character.StrengthBuff = active ? Bonus : (short)0;
        }
    }
}
