using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Assets.Scripts.Areas.Quest.Enums;
using Assets.Scripts.Areas.Quest.Models;
using Assets.Scripts.Areas.Shared.Mono;

namespace Assets.Scripts.Areas.Quest
{
    public class QuestManager : Singleton<QuestManager>
    {
        public QuestDto[] Quests { get; private set; }

        public IList<CharacterQuestDto> CharacterQuests { get; private set; }

        public async UniTask LoadAsync()
        {
            var result = await UnityWebRequestHelper.ExecuteGetAsync<GetQuestsDto>("Quests");

            Quests = result.Quests;
        }

        public async UniTask LoadAsync(int characterId)
        {
            var result = await UnityWebRequestHelper.ExecuteGetAsync<GetCharacterQuestsDto>($"CharacterQuests?CharacterId={characterId}");

            CharacterQuests = result.CharacterQuests;
        }

        public async UniTask<CharacterQuestDto> AcceptCharacterQuestAsync(QuestEnum questId)
        {
            return await UnityWebRequestHelper.ExecutePostAsync<CharacterQuestDto>("CharacterQuests/Accept", new AcceptCharacterQuestCommand
            {
                QuestId = questId
            });
        }

        public async UniTask<AddCharacterQuestProgressDto> AddCharacterQuestProgresAsync(int progress, int characterQuestId, string clientToken)
        {
            return await UnityWebRequestHelper.ExecutePostAsync<AddCharacterQuestProgressDto>("CharacterQuests/Progress", new AddCharacterQuestProgressCommand
            {
                CharacterQuestId = characterQuestId,
                Progress = progress,
            }, clientToken);
        }

        public async UniTask<CheckCharacterQuestProgressDto> CheckProgressAsync(QuestEnum questId, int progress, int characterId, string clientToken)
        {
            return await UnityWebRequestHelper.ExecutePostAsync<CheckCharacterQuestProgressDto>("CharacterQuests/CheckProgress", new CheckCharacterQuestProgressCommand
            {
                QuestId = questId,
                Progress = progress,
                CharacterId = characterId,
            }, clientToken);
        }

        public async UniTask<CompleteCharacterQuestDto> CompleteAsync(int characterQuestId, string clientToken)
        {
            return await UnityWebRequestHelper.ExecutePostAsync<CompleteCharacterQuestDto>("CharacterQuests/Complete", new CompleteCharacterQuestCommand
            {
                CharacterQuestId = characterQuestId,
            }, clientToken);
        }
    }
}