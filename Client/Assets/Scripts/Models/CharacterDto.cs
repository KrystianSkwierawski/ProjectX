using System;

namespace Assets.Scripts.Models
{
    [Serializable]
    public class CharacterDto
    {
        public string name;

        public byte level;

        public byte skillPoints;

        public int health;
    }
}