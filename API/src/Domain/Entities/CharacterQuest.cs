using System.ComponentModel.DataAnnotations.Schema;
using ProjectX.Domain.Enums;

namespace ProjectX.Domain.Entities;
public class CharacterQuest
{
    public int Id { get; set; }

    public int QuestId { get; set; }

    public int CharacterId { get; set; }

    public CharacterQuestStatusEnum Status { get; set; }

    public int Progress { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime ModDate { get; set; }

    public DateTime EndDate { get; set; }

    [ForeignKey(nameof(CharacterId))]
    [InverseProperty(nameof(Character.CharacterQuests))]
    public virtual Character Character { get; set; }

    [ForeignKey(nameof(QuestId))]
    [InverseProperty(nameof(Quest.CharacterQuests))]
    public virtual Quest Quest { get; set; }
}
