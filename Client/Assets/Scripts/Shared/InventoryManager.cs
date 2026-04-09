using System.Linq;
using Assets.Scripts.Models;
using Cysharp.Threading.Tasks;

namespace Assets.Scripts.Shared
{
    public class InventoryManager : Singleton<InventoryManager>
    {
        public CharacterInventoryDto Dto { get; private set; }

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
            var slot = Dto.inventory.items
                .Where(x => x.type == item.type)
                .FirstOrDefault();

            // TODO: out of slots?

            if (slot == null)
            {
                Dto.inventory.items.Add(item);

                return;
            }

            slot.count += item.count;
        }

        public void Remove(InventoryItemDto item)
        {
            var slot = Dto.inventory.items
                .Where(x => x.type == item.type)
                .Where(x => x.count >= item.count)
                .First();

            // TODO: multiple stacks?

            if (slot.count == item.count)
            {
                Dto.inventory.items.Remove(slot);

                return;
            }

            slot.count -= item.count;
        }
    }
}
