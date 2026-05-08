using System.Linq;
using Assets.Scripts.Enums;
using Assets.Scripts.Models;
using Cysharp.Threading.Tasks;

namespace Assets.Scripts.Shared
{
    public class InventoryManager : Singleton<InventoryManager>
    {
        public CharacterInventoryDto Dto { get; private set; }

        public int Currency => Dto.Inventory.Items
            .Where(x => x.Type == InventoryItemEnum.Currency)
            .Select(x => x.Count)
            .Sum();

        public async UniTask LoadAsync()
        {
            Dto = await UnityWebRequestHelper.ExecuteGetAsync<CharacterInventoryDto>("CharacterInventories?CharacterId=1");
        }

        public async UniTask UpdateAsync(UpdateCharacterInventoryCommand request, string clientToken)
        {
            await UnityWebRequestHelper.ExecutePostAsync<EmptyResponse>("CharacterInventories", request, clientToken);
        }

        public void Add(InventoryItemDto item)
        {
            var slot = Dto.Inventory.Items
                .Where(x => x.Type == item.Type)
                .FirstOrDefault();

            // TODO: out of slots?

            if (slot == null)
            {
                Dto.Inventory.Items.Add(item);

                return;
            }

            slot.Count += item.Count;
        }

        public void Remove(InventoryItemDto item)
        {
            // TODO: remove from multiple stacks!
            var slot = Dto.Inventory.Items
                .Where(x => x.Type == item.Type)
                .Where(x => x.Count >= item.Count)
                .First();

            // TODO: multiple stacks?

            if (slot.Count == item.Count)
            {
                Dto.Inventory.Items.Remove(slot);

                return;
            }

            slot.Count -= item.Count;
        }
    }
}
