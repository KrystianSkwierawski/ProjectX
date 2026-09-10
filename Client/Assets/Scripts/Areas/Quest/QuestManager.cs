using System.Collections.Generic;
using System.Threading;
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

        public async UniTask LoadCharacterQuestsAsync(int characterId)
        {
            var result = await UnityWebRequestHelper.ExecuteGetAsync<GetCharacterQuestsDto>($"CharacterQuests?CharacterId={characterId}");

            CharacterQuests = result.CharacterQuests;
        }

        public async UniTask<CharacterQuestDto> AcceptCharacterQuestAsync(
            QuestEnum questId,
            string playerSessionId,
            CancellationToken cancellationToken = default)
        {
            return await UnityWebRequestHelper.ExecutePostAsync<CharacterQuestDto>("CharacterQuests/Accept", new AcceptCharacterQuestCommand
            {
                QuestId = questId
            }, playerSessionId, cancellationToken: cancellationToken);
        }

        public async UniTask<AddCharacterQuestProgressDto> AddCharacterQuestProgressAsync(int progress, int characterQuestId, string playerSessionId)
        {
            return await UnityWebRequestHelper.ExecutePostAsync<AddCharacterQuestProgressDto>("CharacterQuests/Progress", new AddCharacterQuestProgressCommand
            {
                CharacterQuestId = characterQuestId,
                Progress = progress,
            }, playerSessionId);
        }

        public async UniTask<CheckCharacterQuestProgressDto> CheckProgressAsync(
            QuestEnum questId,
            int progress,
            string playerSessionId,
            CancellationToken cancellationToken = default)
        {
            return await UnityWebRequestHelper.ExecutePostAsync<CheckCharacterQuestProgressDto>("CharacterQuests/CheckProgress", new CheckCharacterQuestProgressCommand
            {
                QuestId = questId,
                Progress = progress,
            }, playerSessionId, cancellationToken: cancellationToken);
        }

        public async UniTask<CompleteCharacterQuestDto> CompleteAsync(
            int characterQuestId,
            string playerSessionId,
            CancellationToken cancellationToken = default)
        {
            return await UnityWebRequestHelper.ExecutePostAsync<CompleteCharacterQuestDto>("CharacterQuests/Complete", new CompleteCharacterQuestCommand
            {
                CharacterQuestId = characterQuestId,
            }, playerSessionId, cancellationToken: cancellationToken);
        }
    }
}
