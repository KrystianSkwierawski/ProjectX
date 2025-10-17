using System;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class QuestNpc : MonoBehaviour
{
    [SerializeReference] private int _id = 1;

    private QuestoDto _quest;

    private async void Start()
    {
        _quest = await GetQuestAsync(_id);
    }

    private async UniTask<QuestoDto> GetQuestAsync(int id)
    {
        using var request = UnityWebRequest.Get($"https://localhost:5001/api/Quests/{id}");

        request.SetRequestHeader("Authorization", $"Bearer {TokenManager.Instance.Token}");

        await request.SendWebRequest();

        Debug.Log($"GetQuest result: {request.result}");
        Debug.Log($"GetQuest text: {request.downloadHandler.text}");

        if (request.result == UnityWebRequest.Result.Success)
        {
            return JsonUtility.FromJson<QuestoDto>(request.downloadHandler.text);
        }

        throw new Exception(request.error);
    }

    private class QuestoDto
    {
        public QuestTypeEnum type;

        public string title;

        public string description;

        public string statusText;

        public int reward;
    }

    private enum QuestTypeEnum : byte
    {
        Indefinite,

        Kill,

        Epxlore,

        Find,

        Gather,

        Drop,

        Collect
    }
}
