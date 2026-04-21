using Assets.Scripts.Enums;

namespace Assets.Scripts.Models
{
    public class AddCharacterQuestProgressDto
    {
        public CharacterQuestStatusEnum Status { get; set; }

        public int Reward { get; set; }
    }
}