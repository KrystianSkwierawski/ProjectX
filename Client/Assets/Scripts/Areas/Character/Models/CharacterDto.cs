using System.Collections.Generic;
using Assets.Scripts.Areas.Character.Enums;
using Assets.Scripts.Areas.Inventory.Enums;
using Unity.Netcode;

namespace Assets.Scripts.Areas.Character.Models
{
    public class CharacterDto : INetworkSerializable
    {
        public string Name { get; set; }

        public IDictionary<ExperienceTypeEnum, byte> Levels { get; set; }

        public int Health { get; set; }

        public short Strength { get; set; } = 15;

        public short Agility { get; set; } = 15;

        public short Stamina { get; set; } = 5;

        public short Intelligence { get; set; } = 5;

        public short Spirit { get; set; } = 5;

        public short Arrmor { get; set; } = 20;

        public InventoryItemEnum Helmet { get; set; }

        public InventoryItemEnum Chest { get; set; }

        public InventoryItemEnum Boots { get; set; }

        public InventoryItemEnum Weapon { get; set; }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            if (serializer.IsReader)
            {
                // Read Name
                string name = default;
                serializer.SerializeValue(ref name);
                Name = name;

                // Read Health
                int health = default;
                serializer.SerializeValue(ref health);
                Health = health;

                // Read Levels dictionary
                int count = default;
                serializer.SerializeValue(ref count);

                var dict = new Dictionary<ExperienceTypeEnum, byte>(count);
                for (int i = 0; i < count; i++)
                {
                    int keyInt = default;
                    byte value = default;

                    serializer.SerializeValue(ref keyInt);
                    serializer.SerializeValue(ref value);

                    dict[(ExperienceTypeEnum)keyInt] = value;
                }

                Levels = dict;
            }
            else
            {
                // Write Name
                string name = Name ?? string.Empty;
                serializer.SerializeValue(ref name);

                // Write Health
                int health = Health;
                serializer.SerializeValue(ref health);

                // Write Levels dictionary
                int count = Levels != null ? Levels.Count : 0;
                serializer.SerializeValue(ref count);

                if (count > 0)
                {
                    foreach (var kv in Levels)
                    {
                        int keyInt = (int)kv.Key;
                        byte value = kv.Value;

                        serializer.SerializeValue(ref keyInt);
                        serializer.SerializeValue(ref value);
                    }
                }
            }
        }
    }
}