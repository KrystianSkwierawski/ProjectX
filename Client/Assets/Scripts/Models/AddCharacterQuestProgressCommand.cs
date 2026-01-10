using System;

namespace Assets.Scripts.Models
{
    [Serializable]
    public class AddCharacterQuestProgressCommand
    {
        public int characterQuestId;

        public int progress;
    }
}