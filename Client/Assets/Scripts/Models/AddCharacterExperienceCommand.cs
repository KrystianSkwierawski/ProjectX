using Assets.Scripts.Enums;

namespace Assets.Scripts.Models
{
    public class AddCharacterExperienceCommand
    {
        public int CharacterId { get; set; }

        public int Amount { get; set; }

        public ExperienceTypeEnum type { get; set; }
    }
}