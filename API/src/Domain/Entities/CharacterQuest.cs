using ProjectX.Domain.Enums;

using ProjectX.Domain.Common;

namespace ProjectX.Domain.Entities;
public class CharacterQuest : BaseAuditableEntity
{
    public int Id { get; set; }

    public QuestEnum QuestId { get; set; }

    public int CharacterId { get; set; }

    public CharacterQuestStatusEnum Status { get; set; }

    public int Progress { get; set; }

    public DateTimeOffset StartDate { get; set; }

    public DateTimeOffset EndDate { get; set; }

    public virtual Character Character { get; set; }

    public virtual Quest Quest { get; set; }
}
