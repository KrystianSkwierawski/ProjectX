using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class ExperienceService : IExperienceService
{
    private readonly ITokenManagerService _tokenManagerService;

    public ExperienceService(ITokenManagerService tokenManagerService)
    {
        _tokenManagerService = tokenManagerService;
    }

    public async UniTask<AddCharacterExperienceDto> Add(string token, ulong clientId)
    {
        using var request = UnityWebRequest.Post("https://localhost:5001/api/CharacterExperiences", JsonUtility.ToJson(new AddCharacterExperienceCommand
        {
            amount = 1000,
            type = ExperienceTypeEnum.Combat,
            clientToken = token
        }), "application/json");

        request.SetRequestHeader("Authorization", $"Bearer {_tokenManagerService.Token}");

        await request.SendWebRequest();

        Debug.Log($"Add result: {request.result}");
        Debug.Log($"Add text: {request.downloadHandler.text}");

        if (request.result == UnityWebRequest.Result.Success)
        {
            return JsonUtility.FromJson<AddCharacterExperienceDto>(request.downloadHandler.text);
        }

        throw new Exception(request.error);
    }
}

public interface IExperienceService
{
    UniTask<AddCharacterExperienceDto> Add(string token, ulong clientId);
}