using System;
using Unity.Netcode;
using UnityEngine;
using Assets.Scripts.Areas.Inventory.Enums;

namespace Assets.Scripts.Areas.Inventory.Models
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