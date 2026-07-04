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

        public short Strength { get; set; }

        public short Agility { get; set; }

        public short Stamina { get; set; }

        public short Intelligence { get; set; }

        public short Spirit { get; set; }

        public short Arrmor { get; set; }

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

                short strength = default;
                serializer.SerializeValue(ref strength);
                Strength = strength;

                short agility = default;
                serializer.SerializeValue(ref agility);
                Agility = agility;

                short stamina = default;
                serializer.SerializeValue(ref stamina);
                Stamina = stamina;

                short intelligence = default;
                serializer.SerializeValue(ref intelligence);
                Intelligence = intelligence;

                short spirit = default;
                serializer.SerializeValue(ref spirit);
                Spirit = spirit;

                short arrmor = default;
                serializer.SerializeValue(ref arrmor);
                Arrmor = arrmor;

                int helmet = default;
                serializer.SerializeValue(ref helmet);
                Helmet = (InventoryItemEnum)helmet;

                int chest = default;
                serializer.SerializeValue(ref chest);
                Chest = (InventoryItemEnum)chest;

                int boots = default;
                serializer.SerializeValue(ref boots);
                Boots = (InventoryItemEnum)boots;

                int weapon = default;
                serializer.SerializeValue(ref weapon);
                Weapon = (InventoryItemEnum)weapon;

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

                short strength = Strength;
                serializer.SerializeValue(ref strength);

                short agility = Agility;
                serializer.SerializeValue(ref agility);

                short stamina = Stamina;
                serializer.SerializeValue(ref stamina);

                short intelligence = Intelligence;
                serializer.SerializeValue(ref intelligence);

                short spirit = Spirit;
                serializer.SerializeValue(ref spirit);

                short arrmor = Arrmor;
                serializer.SerializeValue(ref arrmor);

                int helmet = (int)Helmet;
                serializer.SerializeValue(ref helmet);

                int chest = (int)Chest;
                serializer.SerializeValue(ref chest);

                int boots = (int)Boots;
                serializer.SerializeValue(ref boots);

                int weapon = (int)Weapon;
                serializer.SerializeValue(ref weapon);

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
