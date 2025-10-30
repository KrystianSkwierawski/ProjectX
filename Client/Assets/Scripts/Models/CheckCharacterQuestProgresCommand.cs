using System;

namespace Assets.Scripts.Models
{
    [Serializable]
    public class CheckCharacterQuestProgresCommand
    {
        public int characterId;

        public string gameObjectName;

        public int progres;
    }
}