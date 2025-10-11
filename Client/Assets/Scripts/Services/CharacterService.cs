using System;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public interface ICharacterService
{
    UniTask GetDetailsAsync(GameObject playerCanvas);

    UniTask GetTransformAsync(Transform transform);

    UniTask SaveTransformAsync(Transform transform, string token);
}

public class CharacterService : ICharacterService
{
    private readonly ITokenManagerService _tokenManagerService;

    public CharacterService(ITokenManagerService tokenManagerService)
    {
        _tokenManagerService = tokenManagerService;
    }

    public async UniTask GetDetailsAsync(GameObject playerCanvas)
    {
        using var request = UnityWebRequest.Get("https://localhost:5001/api/Characters/1");

        request.SetRequestHeader("Authorization", $"Bearer {_tokenManagerService.Token}");

        await request.SendWebRequest();

        Debug.Log($"GetCharacter result: {request.result}");
        Debug.Log($"GetCharacter text: {request.downloadHandler.text}");

        if (request.result == UnityWebRequest.Result.Success)
        {
            var result = JsonUtility.FromJson<CharacterDto>(request.downloadHandler.text);

            playerCanvas.transform.Find("Player/Name").GetComponent<TextMeshProUGUI>().text = result.name;
            playerCanvas.transform.Find("Player/HealthPoints").GetComponent<TextMeshProUGUI>().text = result.health.ToString();
            playerCanvas.transform.Find("Player/Level").GetComponent<TextMeshProUGUI>().text = $"Level: {result.level}";
        }
    }

    public async UniTask GetTransformAsync(Transform transform)
    {
        using var request = UnityWebRequest.Get("https://localhost:5001/api/CharacterTransforms");

        request.SetRequestHeader("Authorization", $"Bearer {_tokenManagerService.Token}");

        await request.SendWebRequest();

        Debug.Log($"GetTransform result: {request.result}");
        Debug.Log($"GetTransformA text: {request.downloadHandler.text}");

        if (request.result == UnityWebRequest.Result.Success)
        {
            var result = JsonUtility.FromJson<CharacterTransformDto>(request.downloadHandler.text);
            transform.position = new Vector3(result.positionX, result.positionY, result.positionZ);
            transform.rotation.Set(0, result.rotationY, 0, 0);
        }
    }

    public async UniTask SaveTransformAsync(Transform transform, string token)
    {
        using var request = UnityWebRequest.Post("https://localhost:5001/api/CharacterTransforms", JsonUtility.ToJson(new CharacterTransformDto
        {
            positionX = transform.position.x,
            positionY = transform.position.y,
            positionZ = transform.position.z,
            rotationY = transform.rotation.y,
            clientToken = token
        }), "application/json");

        request.SetRequestHeader("Authorization", $"Bearer {_tokenManagerService.Token}");

        await request.SendWebRequest();

        //Debug.Log($"SaveTransform result: {request.result}");
        //Debug.Log($"SaveTransform text: {request.downloadHandler.text}");
    }

    [Serializable]
    private class CharacterTransformDto // wydziel osobno na get i save
    {
        public int characterId;

        public float positionX;

        public float positionY;

        public float positionZ;

        public float rotationY;

        public string clientToken;
    }

    [Serializable]
    private class CharacterDto
    {
        public string name;

        public byte level;

        public byte skillPoints;

        public int health;
    }
}
