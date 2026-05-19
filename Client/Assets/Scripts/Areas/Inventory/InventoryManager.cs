using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using Assets.Scripts.Areas.Inventory.Models;
using Assets.Scripts.Areas.Shared.Models;
using Assets.Scripts.Areas.Shared.Mono;

namespace Assets.Scripts.Areas.Inventory
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
            var sum = Dto.Inventory.Items
                .Where(x => x.Type == item.Type)
                .Select(x => x.Count)
                .Sum();

            if (sum < item.Count)
            {
                throw new Exception($"Not enough items of type {item.Type} to remove. Current count: {sum}, requested count: {item.Count}");
            }

            var slot = Dto.Inventory.Items
                .Where(x => x.Type == item.Type)
                .First();

            var diff = slot.Count - item.Count;

            if (diff <= 0)
            {
                Dto.Inventory.Items.Remove(slot);

                if (diff < 0)
                {
                    Remove(new InventoryItemDto
                    {
                        Count = Math.Abs(diff),
                        Type = item.Type
                    });
                }

                return;
            }

            slot.Count -= item.Count;
        }
    }
}
