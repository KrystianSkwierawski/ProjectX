using System;
using Unity.Netcode;

namespace Assets.Scripts.Models
{
    public class UpdateCharacterInventoryCommand : INetworkSerializable
    {
        public int CharacterId { get; set; }

        public InventoryItemDto[] Add { get; set; }

        public InventoryItemDto[] Remove { get; set; }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            // Serialize CharacterId via a local so we can pass by ref
            int characterId = CharacterId;
            serializer.SerializeValue(ref characterId);
            if (serializer.IsReader)
            {
                CharacterId = characterId;
            }

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
}