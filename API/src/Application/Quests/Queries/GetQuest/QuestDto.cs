using ProjectX.Domain.Enums;

namespace ProjectX.Application.Quests.Queries.GetQuest;
public class QuestoDto
{
    public QuestTypeEnum Type { get; set; }

    public required string Title { get; set; }

    public required string Description { get; set; }

    public required string StatusText { get; set; }

    public int Reward { get; set; }

    public override string ToString()
    {
        return $"{nameof(QuestoDto)} {{ Type = {Type}, Title = {Title}, Description = {Description}, StatusText = {StatusText}, Reward = {Reward} }}";
    }
}
