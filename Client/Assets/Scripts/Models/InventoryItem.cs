using System;
using Assets.Scripts.Enums;
using Unity.Netcode;

namespace Assets.Scripts.Models
{
    [Serializable]
    public class InventoryItem : INetworkSerializable // TODO: NetworkSerializableModel
    {
        public CharacterInventoryTypeEnum type;

        public int count;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref type);
            serializer.SerializeValue(ref count);
        }
    }
}

