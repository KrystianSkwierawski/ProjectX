using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

public sealed class QuestManager
{
    private static readonly QuestManager _instance = new QuestManager();

    private QuestManager()
    {

    }

    public static QuestManager Instance
    {
        get
        {
            return _instance;
        }
    }

    public UnityEvent AcceptedQuestEvent = new UnityEvent();

    public UnityEvent AddedProgresEvent = new UnityEvent();

    public IList<QuestDto> Quests { get; private set; }

    public IList<CharacterQuestDto> CharacterQuests { get; private set; }

    public async UniTask LoadQuestsAsync()
    {
        using var request = UnityWebRequest.Get($"https://localhost:5001/api/Quests");

        request.SetRequestHeader("Authorization", $"Bearer {TokenManager.Instance.Token}");

        await request.SendWebRequest();

        Debug.Log($"LoadQuests result: {request.result}");
        Debug.Log($"LoadQuests text: {request.downloadHandler.text}");

        if (request.result == UnityWebRequest.Result.Success)
        {
            var result = JsonUtility.FromJson<GetQuestsDto>(request.downloadHandler.text);
            Quests = result.quests;
        }
    }

    public async UniTask LoadCharacterQuestsAsync()
    {
        using var request = UnityWebRequest.Get($"https://localhost:5001/api/CharacterQuests/1");

        request.SetRequestHeader("Authorization", $"Bearer {TokenManager.Instance.Token}");

        await request.SendWebRequest();

        Debug.Log($"LoadCharacterQuests result: {request.result}");
        Debug.Log($"LoadCharacterQuests text: {request.downloadHandler.text}");

        if (request.result == UnityWebRequest.Result.Success)
        {
            var result = JsonUtility.FromJson<GetCharacterQuestsDto>(request.downloadHandler.text);

            CharacterQuests = result.characterQuests;
        }
    }

    public async UniTask AcceptCharacterQuestAsync(int questId)
    {
        using var request = UnityWebRequest.Post($"https://localhost:5001/api/CharacterQuests", JsonUtility.ToJson(new AcceptCharacterQuestCommand
        {
            questId = questId
        }), "application/json");

        request.SetRequestHeader("Authorization", $"Bearer {TokenManager.Instance.Token}");

        await request.SendWebRequest();

        Debug.Log($"AcceptCharacterQuest result: {request.result}");
        Debug.Log($"AcceptCharacterQuest text: {request.downloadHandler.text}");

        if (request.result == UnityWebRequest.Result.Success)
        {
            CharacterQuests.Add(new CharacterQuestDto
            {
                id = int.Parse(request.downloadHandler.text),
                questId = questId,
                progress = 0,
                status = CharacterQuestStatusEnum.Accepted
            });

            AcceptedQuestEvent.Invoke();
        }
    }

    public async UniTask<AddCharacterQuestProgresDto> AddCharacterQuestProgresAsync(int characterQuestId, int progres, string clientToken)
    {
        using var request = UnityWebRequest.Post($"https://localhost:5001/api/CharacterQuests/Progres", JsonUtility.ToJson(new AddCharacterQuestProgresCommand
        {
            characterQuestId = characterQuestId,
            progres = progres,
            clientToken = clientToken
        }), "application/json");

        request.SetRequestHeader("Authorization", $"Bearer {TokenManager.Instance.Token}");

        await request.SendWebRequest();

        Debug.Log($"AcceptCharacterQuest result: {request.result}");
        Debug.Log($"AcceptCharacterQuest text: {request.downloadHandler.text}");

        if (request.result == UnityWebRequest.Result.Success)
        {
            return JsonUtility.FromJson<AddCharacterQuestProgresDto>(request.downloadHandler.text);
        }

        throw new Exception(request.error);
    }
}