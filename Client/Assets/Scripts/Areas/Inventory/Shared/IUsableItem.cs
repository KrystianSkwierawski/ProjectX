using Assets.Scripts.Areas.Character.Models;
using Assets.Scripts.Areas.Inventory.Enums;

namespace Assets.Scripts.Areas.Inventory.Shared
{
    public interface IUsableItem
    {
        InventoryItemEnum Type { get; }

        void Use();

        IUsableItem WithClientToken(string clientToken);

        IUsableItem WithOwnerClientId(ulong ownerClientId);

        IUsableItem WithCharacter(CharacterDto character);
    }
}
