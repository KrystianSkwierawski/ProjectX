using System;
using Unity.Netcode;

namespace Assets.Scripts.Areas.Inventory.Models
{
    public class UpdateCharacterInventoryCommand : INetworkSerializable
    {
        public InventoryItemDto[] Add { get; set; } = Array.Empty<InventoryItemDto>();

        public InventoryItemDto[] Remove { get; set; } = Array.Empty<InventoryItemDto>();

        public int? SplitSlotIndex { get; set; }

        public int? MoveSourceSlotIndex { get; set; }

        public int? MoveTargetSlotIndex { get; set; }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            // Serialize Add array length and elements
            int addLength = Add?.Length ?? 0;
            serializer.SerializeValue(ref addLength);
            if (serializer.IsReader)
            {
                Add = new InventoryItemDto[addLength];
                for (int i = 0; i < addLength; i++)
                {
                    var item = new InventoryItemDto();
                    item.NetworkSerialize(serializer);
                    Add[i] = item;
                }
            }
            else
            {
                for (int i = 0; i < addLength; i++)
                {
                    var item = Add[i];
                    item.NetworkSerialize(serializer);
                }
            }

            // Serialize Remove array length and elements
            int removeLength = Remove?.Length ?? 0;
            serializer.SerializeValue(ref removeLength);
            if (serializer.IsReader)
            {
                Remove = new InventoryItemDto[removeLength];
                for (int i = 0; i < removeLength; i++)
                {
                    var item = new InventoryItemDto();
                    item.NetworkSerialize(serializer);
                    Remove[i] = item;
                }
            }
            else
            {
                for (int i = 0; i < removeLength; i++)
                {
                    var item = Remove[i];
                    item.NetworkSerialize(serializer);
                }
            }
        }
    }

    public class UpdateCharacterInventoryDto
    {
        public UpdateCharacterInventoryStatusEnum Status { get; set; }
    }

    public enum UpdateCharacterInventoryStatusEnum
    {
        Applied,
        InventoryFull
    }
}
