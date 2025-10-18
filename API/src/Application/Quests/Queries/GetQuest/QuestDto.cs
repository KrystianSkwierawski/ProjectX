using ProjectX.Domain.Enums;

namespace ProjectX.Application.Quests.Queries.GetQuest;
public class QuestDto
{
    public int Id { get; set; }

    public QuestTypeEnum Type { get; set; }

    public required string Title { get; set; }

    public required string Description { get; set; }

    public required string StatusText { get; set; }

    public string GameObjectName { get; set; }

    public int Requirement { get; set; }

    public int Reward { get; set; }

    public override string ToString()
    {
        return $"{nameof(QuestDto)} {{ Id = {Id}, Type = {Type}, Title = {Title}, Description = {Description}, StatusText = {StatusText}, GameObjectName = {GameObjectName}, Requirement = {Requirement}, Reward = {Reward} }}";
    }
}
