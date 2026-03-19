using System;
using Unity.Netcode;

namespace Assets.Scripts.Models
{
    [Serializable]
    public class CharacterDto : INetworkSerializable
    {
        public string name;

        public byte mainLevel;

        public int health;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref name);
            serializer.SerializeValue(ref mainLevel);
            serializer.SerializeValue(ref health);
        }
    }
}