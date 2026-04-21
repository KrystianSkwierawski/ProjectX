using ProjectX.Application.Quests.Queries.GetQuest;

namespace ProjectX.Application.Quests.Queries.GetQuests;
public class GetQuestsDto
{
    public required QuestDto[] Quests { get; set; }
}
