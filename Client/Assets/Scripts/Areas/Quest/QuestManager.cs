using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Assets.Scripts.Areas.Quest.Enums;
using Assets.Scripts.Areas.Quest.Models;
using Assets.Scripts.Areas.Shared.Mono;

namespace Assets.Scripts.Areas.Quest
{
    public class QuestManager : Singleton<QuestManager>
    {
        private long _characterQuestsVersion;
        private long _characterQuestsLoadVersion;
        private int? _loadedCharacterId;

        public event System.Action CharacterQuestsChanged;

        public void NotifyCharacterQuestsChanged()
        {
            CharacterQuestsChanged?.Invoke();
        }

        public QuestDto[] Quests { get; private set; }

        public IList<CharacterQuestDto> CharacterQuests { get; private set; }

        public async UniTask LoadAsync()
        {
            var result = await UnityWebRequestHelper.ExecuteGetAsync<GetQuestsDto>("Quests");

            Quests = result.Quests;
        }

        public void InvalidatePendingCharacterQuestLoads()
        {
            _characterQuestsVersion++;
        }

        public async UniTask<bool> LoadCharacterQuestsAsync(int characterId, CancellationToken cancellationToken = default)
        {
            var loadVersion = ++_characterQuestsLoadVersion;

            while (true)
            {
                var stateVersion = _characterQuestsVersion;
                var result = await UnityWebRequestHelper.ExecuteGetAsync<GetCharacterQuestsDto>($"CharacterQuests?CharacterId={characterId}", cancellationToken: cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();

                if (loadVersion != _characterQuestsLoadVersion)
                {
                    UnityEngine.Debug.Log($"Ignoring superseded quest load. CharacterId: {characterId}, Version: {loadVersion}.");

                    return false;
                }

                if (stateVersion != _characterQuestsVersion)
                {
                    UnityEngine.Debug.Log($"Reloading quests after a concurrent server update. CharacterId: {characterId}.");

                    continue;
                }

                ApplyCharacterQuestSnapshot(characterId, result.CharacterQuests);

                return true;
            }
        }

        private void ApplyCharacterQuestSnapshot(int characterId, IList<CharacterQuestDto> snapshot)
        {
            var existing = _loadedCharacterId == characterId && CharacterQuests != null
                ? CharacterQuests.ToDictionary(quest => quest.Id)
                : new Dictionary<int, CharacterQuestDto>();
            var updated = new List<CharacterQuestDto>(snapshot.Count);

            foreach (var incoming in snapshot)
            {
                if (existing.TryGetValue(incoming.Id, out var current) && current.QuestId == incoming.QuestId)
                {
                    current.Status = incoming.Status;
                    current.Progress = incoming.Progress;
                    current.ObjectiveProgress = incoming.ObjectiveProgress;
                    updated.Add(current);
                }
                else
                {
                    updated.Add(incoming);
                }
            }

            CharacterQuests = updated;
            _loadedCharacterId = characterId;

            NotifyCharacterQuestsChanged();
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
            string requirementId,
            int progress,
            string playerSessionId,
            CancellationToken cancellationToken = default)
        {
            return await UnityWebRequestHelper.ExecutePostAsync<CheckCharacterQuestProgressDto>("CharacterQuests/CheckProgress", new CheckCharacterQuestProgressCommand
            {
                QuestId = questId,
                RequirementId = requirementId,
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

        public async UniTask<AbandonCharacterQuestDto> AbandonAsync(
            int characterQuestId,
            string playerSessionId,
            CancellationToken cancellationToken = default)
        {
            return await UnityWebRequestHelper.ExecutePostAsync<AbandonCharacterQuestDto>("CharacterQuests/Abandon", new AbandonCharacterQuestCommand
            {
                CharacterQuestId = characterQuestId
            }, playerSessionId, cancellationToken: cancellationToken);
        }
    }
}
