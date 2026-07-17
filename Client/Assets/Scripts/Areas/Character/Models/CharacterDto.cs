using System.Collections.Generic;
using Assets.Scripts.Areas.Character.Enums;
using Assets.Scripts.Areas.Inventory.Enums;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.TextCore.Text;

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

    public static class CharacterExtensions
    {
        public static float ApplyWeaponDamage(this CharacterDto character, float damage)
        {
            var value = character.WeaponType.GetWeaponCategory() switch
            {
                WeaponCategoryEnum.Sword => character.Strength,
                WeaponCategoryEnum.Wand => character.Intellect,
                WeaponCategoryEnum.Bow => character.Dexterity,
                _ => (short)0
            };

            return Mathf.Max(0f, damage) * GetIncreaseMultiplier(value);
        }

        public static bool IsAttackDodged(this CharacterDto character)
        {
            return Random.Range(0f, 100f) < GetLimitedPercent(character.Dexterity);
        }

        public static float ApplySpeed(this CharacterDto character, float speed)
        {
            return Mathf.Max(0f, speed) * GetIncreaseMultiplier(character.Speed);
        }

        public static int ApplyArmor(this CharacterDto character, int damage)
        {
            var reducedDamage = Mathf.Max(0, damage) * (1f - GetLimitedPercent(character.Armor) / 100f);

            return Mathf.RoundToInt(reducedDamage);
        }

        private static float GetIncreaseMultiplier(short value)
        {
            return 1f + Mathf.Max(0, value) / 100f;
        }

        private static float GetLimitedPercent(short value)
        {
            return Mathf.Clamp(value, 0, 100);
        }
    }
}
