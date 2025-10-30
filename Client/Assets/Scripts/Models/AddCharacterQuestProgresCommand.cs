using System;

namespace Assets.Scripts.Models
{
    [Serializable]
    public class AddCharacterQuestProgresCommand
    {
        public int characterQuestId;

        public int progres;
    }
}