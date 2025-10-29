using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine.Events;

public class QuestManager : Singleton<QuestManager>
{
    public IDictionary<int, QuestNpc> QuestNpcs { get; private set; } = new Dictionary<int, QuestNpc>();

    public UnityEvent<int, CharacterQuestStatusEnum> AddedProgresEvent = new UnityEvent<int, CharacterQuestStatusEnum>();

    public IList<QuestDto> Quests { get; private set; }

    public IList<CharacterQuestDto> CharacterQuests { get; private set; }

    public async UniTask LoadQuestsAsync()
    {
        var result = await UnityWebRequestHelper.ExecuteGetAsync<GetQuestsDto>("Quests");

        Quests = result.quests;
    }

    public async UniTask LoadCharacterQuestsAsync()
    {
        var result = await UnityWebRequestHelper.ExecuteGetAsync<GetCharacterQuestsDto>("CharacterQuests?CharacterId=1");

        CharacterQuests = result.characterQuests;
    }

    public async UniTask<CharacterQuestDto> AcceptCharacterQuestAsync(int questId)
    {
        return await UnityWebRequestHelper.ExecutePostAsync<CharacterQuestDto>("CharacterQuests", new AcceptCharacterQuestCommand
        {
            questId = questId
        });
    }

    public async UniTask<AddCharacterQuestProgresDto> AddCharacterQuestProgresAsync(int characterQuestId, int progres, string clientToken)
    {
        return await UnityWebRequestHelper.ExecutePostAsync<AddCharacterQuestProgresDto>("CharacterQuests/Progres", new AddCharacterQuestProgresCommand
        {
            characterQuestId = characterQuestId,
            progres = progres,
        }, clientToken);
    }

    public async UniTask<CheckCharacterQuestProgresDto> CheckCharacterQuestProgresAsync(int characterId, string gameObjectName, int progres, string clientToken)
    {
        return await UnityWebRequestHelper.ExecutePostAsync<CheckCharacterQuestProgresDto>("CharacterQuests/CheckProgres", new CheckCharacterQuestProgresCommand
        {
            characterId = characterId,
            gameObjectName = gameObjectName,
            progres = progres,
        }, clientToken);
    }
}