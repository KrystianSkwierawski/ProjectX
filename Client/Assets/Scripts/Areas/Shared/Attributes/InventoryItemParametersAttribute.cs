using System;

namespace Assets.Scripts.Areas.Shared.Attributes
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class InventoryItemParametersAttribute : Attribute
    {
        public int MaxHealth { get; set; }

        public short Strength { get; set; }

        public short Agility { get; set; }

        public short Stamina { get; set; }

        public short Intellect { get; set; }

        public short Spirit { get; set; }

        public short Armor { get; set; }
    }
}
