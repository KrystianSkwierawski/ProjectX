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

        public int MaxHealth { get; set; }

        public short Strength { get; set; }

        public short Dexterity { get; set; }

        public short Speed { get; set; }

        public short Intellect { get; set; }

        public short Armor { get; set; }

        public InventoryItemEnum HelmetType { get; set; }

        public InventoryItemEnum ChestType { get; set; }

        public InventoryItemEnum BootsType { get; set; }

        public InventoryItemEnum WeaponType { get; set; }

        public InventoryItemEnum AmmoType { get; set; }

        public int AmmoCount { get; set; }

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

                int maxHealth = default;
                serializer.SerializeValue(ref maxHealth);
                MaxHealth = maxHealth;

                short strength = default;
                serializer.SerializeValue(ref strength);
                Strength = strength;

                short dexterity = default;
                serializer.SerializeValue(ref dexterity);
                Dexterity = dexterity;

                short speed = default;
                serializer.SerializeValue(ref speed);
                Speed = speed;

                short intellect = default;
                serializer.SerializeValue(ref intellect);
                Intellect = intellect;

                short armor = default;
                serializer.SerializeValue(ref armor);
                Armor = armor;

                int helmet = default;
                serializer.SerializeValue(ref helmet);
                HelmetType = (InventoryItemEnum)helmet;

                int chest = default;
                serializer.SerializeValue(ref chest);
                ChestType = (InventoryItemEnum)chest;

                int boots = default;
                serializer.SerializeValue(ref boots);
                BootsType = (InventoryItemEnum)boots;

                int weapon = default;
                serializer.SerializeValue(ref weapon);
                WeaponType = (InventoryItemEnum)weapon;

                int ammoType = default;
                serializer.SerializeValue(ref ammoType);
                AmmoType = (InventoryItemEnum)ammoType;

                int ammoCount = default;
                serializer.SerializeValue(ref ammoCount);
                AmmoCount = ammoCount;

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

                int maxHealth = MaxHealth;
                serializer.SerializeValue(ref maxHealth);

                short strength = Strength;
                serializer.SerializeValue(ref strength);

                short dexterity = Dexterity;
                serializer.SerializeValue(ref dexterity);

                short speed = Speed;
                serializer.SerializeValue(ref speed);

                short intellect = Intellect;
                serializer.SerializeValue(ref intellect);

                short armor = Armor;
                serializer.SerializeValue(ref armor);

                int helmet = (int)HelmetType;
                serializer.SerializeValue(ref helmet);

                int chest = (int)ChestType;
                serializer.SerializeValue(ref chest);

                int boots = (int)BootsType;
                serializer.SerializeValue(ref boots);

                int weapon = (int)WeaponType;
                serializer.SerializeValue(ref weapon);

                int ammoType = (int)AmmoType;
                serializer.SerializeValue(ref ammoType);

                int ammoCount = AmmoCount;
                serializer.SerializeValue(ref ammoCount);

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
