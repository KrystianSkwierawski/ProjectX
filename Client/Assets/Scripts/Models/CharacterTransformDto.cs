using System;

namespace Assets.Scripts.Models
{
    [Serializable]
    public class CharacterTransformDto
    {
        public int characterId;

        public float positionX;

        public float positionY;

        public float positionZ;

        public float rotationY;
    }
}