using Assets.Scripts.Areas.Character.Enums;

namespace Assets.Scripts.Areas.Character.Models
{
    public class AddCharacterExperienceCommand
    {
        public int CharacterId { get; set; }

        public int Amount { get; set; }

        public ExperienceTypeEnum type { get; set; }
    }
}