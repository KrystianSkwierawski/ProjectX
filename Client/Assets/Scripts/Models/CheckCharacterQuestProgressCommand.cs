using System;

namespace Assets.Scripts.Models
{
    [Serializable]
    public class CheckCharacterQuestProgressCommand
    {
        public int characterId;

        public string gameObjectName;

        public int progress;
    }
}