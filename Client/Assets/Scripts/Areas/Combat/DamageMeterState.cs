using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Areas.Inventory.Enums;

namespace Assets.Scripts.Areas.Combat
{
    public sealed class DamageMeterEntry
    {
        public int CharacterId { get; }
        public string Nickname { get; private set; }
        public InventoryItemEnum WeaponType { get; }
        public double Damage { get; private set; }

        internal DamageMeterEntry(int characterId, string nickname, InventoryItemEnum weaponType)
        {
            CharacterId = characterId;
            Nickname = nickname;
            WeaponType = weaponType;
        }

        internal void Add(string nickname, float damage)
        {
            Nickname = nickname;
            Damage += damage;
        }
    }

    public static class DamageMeterState
    {
        // Keep first-seen order separately so equal totals always have stable ordering.
        private static readonly List<DamageMeterEntry> _entries = new List<DamageMeterEntry>();

        public static event Action Changed;

        public static IReadOnlyList<DamageMeterEntry> Entries { get; private set; } = Array.Empty<DamageMeterEntry>();
        public static double TotalDamage { get; private set; }

        public static void Reset()
        {
            _entries.Clear();
            Entries = Array.Empty<DamageMeterEntry>();
            TotalDamage = 0;

            Changed?.Invoke();
        }

        public static void Record(int characterId, string nickname, InventoryItemEnum weaponType, float damage)
        {
            if (characterId <= 0 || damage <= 0 || float.IsNaN(damage) || float.IsInfinity(damage))
            {
                return;
            }

            var entry = _entries
                .Where(x => x.CharacterId == characterId && x.WeaponType == weaponType)
                .FirstOrDefault();

            if (entry == null)
            {
                entry = new DamageMeterEntry(characterId, nickname, weaponType);
                _entries.Add(entry);
            }

            entry.Add(nickname, damage);
            TotalDamage += damage;
            Entries = _entries
                .OrderByDescending(x => x.Damage)
                .ToList()
                .AsReadOnly();

            Changed?.Invoke();
        }
    }
}
