using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public sealed class ExperienceManager : BaseManager<ExperienceManager>
{
    public async UniTask<AddCharacterExperienceDto> AddExperienceAsync(ExperienceTypeEnum type, string token, ulong clientId, int? characterQuestId = null)
    {
        using var request = UnityWebRequest.Post("https://localhost:5001/api/CharacterExperiences", JsonUtility.ToJson(new AddCharacterExperienceCommand
        {
            characterId = 1,
            characterQuestId = characterQuestId ?? 0,
            type = type,
            clientToken = token
        }), "application/json");

        request.SetRequestHeader("Authorization", $"Bearer {TokenManager.Instance.Token}");

        await request.SendWebRequest();

        Debug.Log($"AddExperience result: {request.result}");
        Debug.Log($"AddExperience text: {request.downloadHandler.text}");

        if (request.result == UnityWebRequest.Result.Success)
        {
            return JsonUtility.FromJson<AddCharacterExperienceDto>(request.downloadHandler.text);
        }

        throw new Exception(request.error);
    }
}