using System.ComponentModel.DataAnnotations.Schema;
using ProjectX.Domain.Enums;

namespace ProjectX.Domain.Entities;
public class Quest
{
    public Quest()
    {
        CharacterQuests = new HashSet<CharacterQuest>();
    }

    public int Id { get; set; }

    public int? PreviousQuestId { get; set; }

    public QuestTypeEnum Type { get; set; }

    public required string Title { get; set; }

    public required string Description { get; set; }

    public required string CompleteDescription { get; set; }

    public required string StatusText { get; set; }

    public string GameObjectName { get; set; }

    public int Requirement { get; set; }

    public int Reward { get; set; }

    public DateTime ModDate { get; set; }

    [InverseProperty(nameof(CharacterQuest.Quest))]
    public virtual ICollection<CharacterQuest> CharacterQuests { get; set; }
}
