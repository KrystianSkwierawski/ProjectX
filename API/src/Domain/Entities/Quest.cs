using ProjectX.Domain.Enums;

using ProjectX.Domain.Common;

namespace ProjectX.Domain.Entities;
public class Quest : BaseAuditableEntity
{
    public Quest()
    {
        CharacterQuests = new HashSet<CharacterQuest>();
    }

    public QuestEnum Id { get; set; }

    public string Name { get; set; }

    public QuestEnum PreviousQuestId { get; set; }

    public QuestTypeEnum Type { get; set; }

    public string GameObjectName { get; set; }

    public int Requirement { get; set; }

    public int Reward { get; set; }

    public StatusEnum Status { get; set; }

    public virtual ICollection<CharacterQuest> CharacterQuests { get; set; }
}
