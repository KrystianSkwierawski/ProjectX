using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

public sealed class QuestManager
{
    public IDictionary<int, QuestNpc> QuestNpcs { get; private set; } = new Dictionary<int, QuestNpc>();

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

    public UnityEvent<int, CharacterQuestStatusEnum> AddedProgresEvent = new UnityEvent<int, CharacterQuestStatusEnum>();

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
        using var request = UnityWebRequest.Get($"https://localhost:5001/api/CharacterQuests?CharacterId=1");

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

    public async UniTask<CharacterQuestDto> AcceptCharacterQuestAsync(int questId)
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
            return new CharacterQuestDto
            {
                id = int.Parse(request.downloadHandler.text),
                questId = questId,
                progress = 0,
                status = CharacterQuestStatusEnum.Accepted
            };
        }

        throw new Exception(request.error);
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

    public async UniTask<CheckCharacterQuestProgresDto> CheckCharacterQuestProgresAsync(int characterId, string gameObjectName, int progres, string token)
    {
        using var request = UnityWebRequest.Post("https://localhost:5001/api/CharacterQuests/CheckProgres", JsonUtility.ToJson(new CheckCharacterQuestProgresCommand
        {
            characterId = characterId,
            gameObjectName = gameObjectName,
            progres = progres,
            clientToken = token
        }), "application/json");

        request.SetRequestHeader("Authorization", $"Bearer {TokenManager.Instance.Token}");

        await request.SendWebRequest();

        Debug.Log($"CheckCharacterProgres result: {request.result}");
        Debug.Log($"CheckCharacterProgres text: {request.downloadHandler.text}");

        if (request.result == UnityWebRequest.Result.Success)
        {
            return JsonUtility.FromJson<CheckCharacterQuestProgresDto>(request.downloadHandler.text);
        }

        throw new Exception(request.error);
    }
}