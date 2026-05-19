using Assets.Scripts.Areas.Quest.Enums;

namespace Assets.Scripts.Areas.Quest.Models
{
    public class AddCharacterQuestProgressDto
    {
        public CharacterQuestStatusEnum Status { get; set; }

        public int Reward { get; set; }
    }
}