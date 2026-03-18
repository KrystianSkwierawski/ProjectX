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

        public void Add(InventoryItemDto item)
        {
            var slot = Dto.inventory.items
                .Where(x => x.type == item.type)
                .FirstOrDefault();

            if (slot == null && Dto.inventory.items.Count >= Dto.count)
            {
                // TODO: out of slots
                return;
            }

            if (slot != null)
            {
                slot.count += item.count;

                return;
            }

            Dto.inventory.items.Add(item);
        }

        public void Remove(InventoryItemDto item)
        {
            var slot = Dto.inventory.items
                .Where(x => x.type == item.type)
                .Where(x => x.count >= item.count)
                .First();

            if (slot.count == item.count)
            {
                Dto.inventory.items.Remove(slot);

                return;
            }

            slot.count -= item.count;
        }
    }
}
