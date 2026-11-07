using ProjectX.Domain.Common;
using ProjectX.Domain.Enums;

namespace ProjectX.Domain.Entities;

public class Quest : BaseAuditableEntity
{
    public QuestEnum Id { get; set; }

    public required string Name { get; set; }

    public QuestEnum PreviousQuestId { get; set; }

    public StatusEnum Status { get; set; }

    public virtual ICollection<CharacterQuest> CharacterQuests { get; private set; } = new HashSet<CharacterQuest>();
}
