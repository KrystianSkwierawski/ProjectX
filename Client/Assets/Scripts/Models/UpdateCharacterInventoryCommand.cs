using System;
using System.Collections.Generic;
using Unity.Netcode;

namespace Assets.Scripts.Models
{
    [Serializable]
    public class UpdateCharacterInventoryCommand : INetworkSerializable
    {
        public int characterId;

        public List<InventoryItemDto> add = new List<InventoryItemDto>();

        public List<InventoryItemDto> remove = new List<InventoryItemDto>();

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref characterId);

            // Serialize 'add' list
            int addCount = add != null ? add.Count : 0;
            serializer.SerializeValue(ref addCount);

            if (serializer.IsReader)
            {
                add = new List<InventoryItemDto>(addCount);
            }

            for (int i = 0; i < addCount; i++)
            {
                if (serializer.IsReader)
                {
                    var element = new InventoryItemDto();
                    serializer.SerializeValue(ref element);
                    add.Add(element);
                }
                else
                {
                    var element = add[i];
                    serializer.SerializeValue(ref element);
                }
            }

            // Serialize 'remove' list
            int removeCount = remove != null ? remove.Count : 0;
            serializer.SerializeValue(ref removeCount);

            if (serializer.IsReader)
            {
                remove = new List<InventoryItemDto>(removeCount);
            }

            for (int i = 0; i < removeCount; i++)
            {
                if (serializer.IsReader)
                {
                    var element = new InventoryItemDto();
                    serializer.SerializeValue(ref element);
                    remove.Add(element);
                }
                else
                {
                    var element = remove[i];
                    serializer.SerializeValue(ref element);
                }
            }
        }
    }
}