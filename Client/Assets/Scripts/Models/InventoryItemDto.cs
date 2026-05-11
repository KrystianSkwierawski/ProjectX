using System;
using Assets.Scripts.Enums;
using Unity.Netcode;
using UnityEngine;

namespace Assets.Scripts.Models
{
    [Serializable]
    public class InventoryItemDto : INetworkSerializable
    {
        [SerializeField]
        private InventoryItemEnum _type;

        public InventoryItemEnum Type
        {
            get
            {
                return _type;
            }
            set
            {
                _type = value;
            }
        }

        [SerializeField]
        private int _count;

        public int Count
        {
            get
            {
                return _count;
            }
            set
            {
                _count = value;
            }
        }

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