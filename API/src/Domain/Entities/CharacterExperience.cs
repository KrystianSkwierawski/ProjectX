using ProjectX.Domain.Enums;

namespace ProjectX.Domain.Entities;
public class CharacterExperience
{
    public int Id { get; set; }

    public int CharacterId { get; set; }

    public int Amount { get; set; }

    public ExperienceTypeEnum Type { get; set; }

    public DateTime ModDate { get; set; }

    public virtual Character Character { get; set; }
}
