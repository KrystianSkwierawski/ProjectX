using System;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Networking;

public class CharacterTransform : NetworkBehaviour
{
    private float _period = 0.0f;
    private const float _saveInterval = 5f;

    public override async void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsOwner)
        {
            await GetTransformAsync();
        }
    }

    private void Update()
    {
        if (IsOwner)
        {
            CheckSaveTransform();
        }
    }

    private void CheckSaveTransform()
    {
        if (_period > _saveInterval)
        {
            SaveTransformServerRpc(TokenManager.Instance.Token);
            _period = 0;
        }

        _period += Time.deltaTime;
    }

    [ServerRpc]
    private void SaveTransformServerRpc(string token)
    {
        _ = SaveTransformAsync(token);
    }

    private async UniTask SaveTransformAsync(string token)
    {
        using var request = UnityWebRequest.Post("https://localhost:5001/api/CharacterTransforms", JsonUtility.ToJson(new CharacterTransformDto
        {
            positionX = transform.position.x,
            positionY = transform.position.y,
            positionZ = transform.position.z,
            rotationY = transform.rotation.y,
            clientToken = token
        }), "application/json");
        
        request.SetRequestHeader("Authorization", $"Bearer {TokenManager.Instance.Token}");

        await request.SendWebRequest();

        //Debug.Log($"SaveTransform result: {request.result}");
        //Debug.Log($"SaveTransform text: {request.downloadHandler.text}");
    }

    private async UniTask GetTransformAsync()
    {
        using var request = UnityWebRequest.Get("https://localhost:5001/api/CharacterTransforms");

        request.SetRequestHeader("Authorization", $"Bearer {TokenManager.Instance.Token}");

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

 
}

