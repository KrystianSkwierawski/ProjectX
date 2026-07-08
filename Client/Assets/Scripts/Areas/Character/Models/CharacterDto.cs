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

        public short Agility { get; set; }

        public short Stamina { get; set; }

        public short Intellect { get; set; }

        public short Spirit { get; set; }

        public short Armor { get; set; }

        public InventoryItemEnum Helmet { get; set; }

        public InventoryItemEnum Chest { get; set; }

        public InventoryItemEnum Boots { get; set; }

        public InventoryItemEnum Weapon { get; set; }

        public InventoryItemEnum Ammo { get; set; }

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

                short agility = default;
                serializer.SerializeValue(ref agility);
                Agility = agility;

                short stamina = default;
                serializer.SerializeValue(ref stamina);
                Stamina = stamina;

                short intellect = default;
                serializer.SerializeValue(ref intellect);
                Intellect = intellect;

                short spirit = default;
                serializer.SerializeValue(ref spirit);
                Spirit = spirit;

                short armor = default;
                serializer.SerializeValue(ref armor);
                Armor = armor;

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

                int ammo = default;
                serializer.SerializeValue(ref ammo);
                Ammo = (InventoryItemEnum)ammo;

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

                short agility = Agility;
                serializer.SerializeValue(ref agility);

                short stamina = Stamina;
                serializer.SerializeValue(ref stamina);

                short intellect = Intellect;
                serializer.SerializeValue(ref intellect);

                short spirit = Spirit;
                serializer.SerializeValue(ref spirit);

                short armor = Armor;
                serializer.SerializeValue(ref armor);

                int helmet = (int)Helmet;
                serializer.SerializeValue(ref helmet);

                int chest = (int)Chest;
                serializer.SerializeValue(ref chest);

                int boots = (int)Boots;
                serializer.SerializeValue(ref boots);

                int weapon = (int)Weapon;
                serializer.SerializeValue(ref weapon);

                int ammo = (int)Ammo;
                serializer.SerializeValue(ref ammo);

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
