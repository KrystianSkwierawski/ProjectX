using UnityEngine;

namespace Assets.Scripts.Areas.Character
{
    public static class CharacterStatsCalculator
    {
        public static float ApplyStrength(float damage, short strength)
        {
            return Mathf.Max(0f, damage) * GetIncreaseMultiplier(strength);
        }

        public static bool IsAttackDodged(short dexterity)
        {
            return Random.Range(0f, 100f) < GetLimitedPercent(dexterity);
        }

        public static float ApplySpeed(float speed, short speedStat)
        {
            return Mathf.Max(0f, speed) * GetIncreaseMultiplier(speedStat);
        }

        public static int ApplyArmor(int damage, short armor)
        {
            var reducedDamage = Mathf.Max(0, damage) * (1f - GetLimitedPercent(armor) / 100f);

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
