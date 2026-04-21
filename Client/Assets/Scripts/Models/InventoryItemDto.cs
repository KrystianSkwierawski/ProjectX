using Assets.Scripts.Enums;
using Unity.Netcode;

namespace Assets.Scripts.Models
{
    public class InventoryItemDto : INetworkSerializable
    {
        public InventoryItemEnum Type { get; set; }

        public int Count { get; set; }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            // Serialize enum as int
            int typeInt = (int)Type;
            serializer.SerializeValue(ref typeInt);
            if (serializer.IsReader)
            {
                Type = (InventoryItemEnum)typeInt;
            }

            // Serialize Count
            int count = Count;
            serializer.SerializeValue(ref count);
            if (serializer.IsReader)
            {
                Count = count;
            }
        }
    }
}