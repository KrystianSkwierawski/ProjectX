using System.Collections.Generic;
using Assets.Scripts.Enums;
using Assets.Scripts.Models;
using Cysharp.Threading.Tasks;

namespace Assets.Scripts.Shared
{
    public class QuestManager : Singleton<QuestManager>
    {
        // FIXME: array?
        public IList<QuestDto> Quests { get; private set; }

        public IList<CharacterQuestDto> CharacterQuests { get; private set; }

        public async UniTask LoadAsync()
        {
            var result = await UnityWebRequestHelper.ExecuteGetAsync<GetQuestsDto>("Quests");

            Quests = result.quests;
        }

        public async UniTask LoadAsync(int characterId)
        {
            var result = await UnityWebRequestHelper.ExecuteGetAsync<GetCharacterQuestsDto>($"CharacterQuests?CharacterId={characterId}");

            CharacterQuests = result.characterQuests;
        }

        public async UniTask<CharacterQuestDto> AcceptCharacterQuestAsync(QuestEnum questId)
        {
            return await UnityWebRequestHelper.ExecutePostAsync<CharacterQuestDto>("CharacterQuests/Accept", new AcceptCharacterQuestCommand
            {
                questId = questId
            });
        }

        public async UniTask<AddCharacterQuestProgressDto> AddCharacterQuestProgresAsync(int progress, int characterQuestId, string clientToken)
        {
            return await UnityWebRequestHelper.ExecutePostAsync<AddCharacterQuestProgressDto>("CharacterQuests/Progress", new AddCharacterQuestProgressCommand
            {
                characterQuestId = characterQuestId,
                progress = progress,
            }, clientToken);
        }

        public async UniTask<CheckCharacterQuestProgressDto> CheckProgressAsync(QuestEnum questId, int progress, int characterId, string clientToken)
        {
            return await UnityWebRequestHelper.ExecutePostAsync<CheckCharacterQuestProgressDto>("CharacterQuests/CheckProgress", new CheckCharacterQuestProgressCommand
            {
                questId = questId,
                progress = progress,
                characterId = characterId,
            }, clientToken);
        }

        public async UniTask<CompleteCharacterQuestDto> CompleteAsync(int characterQuestId, string clientToken)
        {
            return await UnityWebRequestHelper.ExecutePostAsync<CompleteCharacterQuestDto>("CharacterQuests/Complete", new CompleteCharacterQuestCommand
            {
                characterQuestId = characterQuestId,
            }, clientToken);
        }
    }
}