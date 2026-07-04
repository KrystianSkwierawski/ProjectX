using System;
using Assets.Scripts.Areas.Character;
using Assets.Scripts.Areas.Character.UI;
using Assets.Scripts.Areas.Inventory.Enums;
using Assets.Scripts.Areas.Shared.Enums;
using Assets.Scripts.Areas.Shared.Mono;

namespace Assets.Scripts.Areas.Inventory.Shared
{
    public class HealthPotionUsableItem : AbstractUsableItem
    {
        public HealthPotionUsableItem(string clientToken, ulong ownerClientId) : base(InventoryItemEnum.HealthPotion, clientToken, ownerClientId)
        {
            
        }

        public override void Use()
        {
            if (UserManager.Instance.Character.Health >= 100)
            {
                return;
            }

            // TODO: set on api
            UserManager.Instance.Character.Health = Math.Min(UserManager.Instance.Character.Health + 20, 100);

#if UNITY_EDITOR
            PlayerUI.Instance.SetHealth(UserManager.Instance.Character.Health);
            AudioManager.Instance.TryPlayOneShot(AudioTypeEnum.Drinking);  
#endif

            base.Use();
        }
    }

}
