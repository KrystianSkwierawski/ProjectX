using System;
using Assets.Scripts.Areas.Character.UI;
using Assets.Scripts.Areas.Inventory.Enums;
using Assets.Scripts.Areas.Shared.Enums;
using Assets.Scripts.Areas.Shared.Mono;

namespace Assets.Scripts.Areas.Inventory.Shared
{
    public class HealthPotionUsableItem : AbstractUsableItem
    {
        public override InventoryItemEnum Type { get; } = InventoryItemEnum.HealthPotion;

        public override void Use()
        {
            if (Character.Health >= 100)
            {
                return;
            }

            // TODO: set on api
            Character.Health = Math.Min(Character.Health + 20, 100);

#if UNITY_EDITOR
            PlayerUI.Instance.SetHealth(Character.Health);
            AudioManager.Instance.TryPlayOneShot(AudioTypeEnum.Drinking);  
#endif

            base.Use();
        }
    }

}
