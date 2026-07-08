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
            var character = UserManager.Instance.Characters[OwnerClientId];

            if (character.Health >= character.MaxHealth)
            {
                return;
            }

            // TODO: set on api
            character.Health = Math.Min(character.Health + 20, character.MaxHealth);

#if UNITY_EDITOR
            PlayerUI.Instance.SetHealth(character.Health);
            AudioManager.Instance.TryPlayOneShot(AudioTypeEnum.Drinking);  
#endif

            base.Use();
        }
    }

}
