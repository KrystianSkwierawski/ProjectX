using ProjectX.Domain.Enums;

namespace ProjectX.Application.Quests.Queries.GetQuest;
public class QuestoDto
{
    public int Id { get; set; }

    public QuestTypeEnum Type { get; set; }

    public required string Title { get; set; }

    public required string Description { get; set; }

    public required string StatusText { get; set; }

    public int Requirement { get; set; }

    public int Reward { get; set; }

    public override string ToString()
    {
        return $"{nameof(QuestoDto)} {{ Id = {Id}, Type = {Type}, Title = {Title}, Description = {Description}, StatusText = {StatusText}, Requirement = {Requirement}, Reward = {Reward} }}";
    }
}
