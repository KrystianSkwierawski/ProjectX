using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class CharacterTransformService : ICharacterService
{
    private readonly ITokenManagerService _tokenManagerService;

    public CharacterTransformService(ITokenManagerService tokenManagerService)
    {
        _tokenManagerService = tokenManagerService;
    }
    public async UniTask<CharacterDto> GetInfoAsync()
    {
        using var request = UnityWebRequest.Get("https://localhost:5001/api/Characters/1");

        request.SetRequestHeader("Authorization", $"Bearer {_tokenManagerService.Token}");

        await request.SendWebRequest();

        Debug.Log($"GetCharacter result: {request.result}");
        Debug.Log($"GetCharacter text: {request.downloadHandler.text}");

        if (request.result == UnityWebRequest.Result.Success)
        {
            return JsonUtility.FromJson<CharacterDto>(request.downloadHandler.text);
        }

        throw new Exception(request.error);
    }

    public async UniTask<CharacterTransformDto> GetTransformAsync(string token)
    {
        using var request = UnityWebRequest.Get("https://localhost:5001/api/CharacterTransforms");

        request.SetRequestHeader("Authorization", $"Bearer {token}");

        await request.SendWebRequest();

        Debug.Log($"GetTransform result: {request.result}");
        Debug.Log($"GetTransform text: {request.downloadHandler.text}");

        if (request.result == UnityWebRequest.Result.Success)
        {
            return JsonUtility.FromJson<CharacterTransformDto>(request.downloadHandler.text);
        }

        throw new Exception(request.error);
    }

    public async UniTask SaveTransformAsync(Vector3 position, float rotationY, string token)
    {
        using var request = UnityWebRequest.Post("https://localhost:5001/api/CharacterTransforms", JsonUtility.ToJson(new CharacterTransformDto
        {
            positionX = position.x,
            positionY = position.y,
            positionZ = position.z,
            rotationY = rotationY,
            clientToken = token
        }), "application/json");

        request.SetRequestHeader("Authorization", $"Bearer {_tokenManagerService.Token}");

        await request.SendWebRequest();

        //Debug.Log($"SaveTransform result: {request.result}");
        //Debug.Log($"SaveTransform text: {request.downloadHandler.text}");
    }
}

public interface ICharacterService
{
    UniTask<CharacterDto> GetInfoAsync();

    UniTask<CharacterTransformDto> GetTransformAsync(string token);

    UniTask SaveTransformAsync(Vector3 position, float rotationY, string token);
}
