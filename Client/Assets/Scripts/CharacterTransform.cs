using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Networking;

public class CharacterTransform : NetworkBehaviour
{
    private float period = 0.0f;
    private const float _saveInterval = 5f;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsOwner)
        {
            StartCoroutine(GetTransform());
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
        if (period > _saveInterval)
        {
            SaveTransformServerRpc(ClientTokenManager.Instance.Token);
            period = 0;
        }

        period += Time.deltaTime;
    }

    [ServerRpc]
    private void SaveTransformServerRpc(string token)
    {
        StartCoroutine(SaveTransform(token));
    }

    private IEnumerator SaveTransform(string token)
    {
        using var request = UnityWebRequest.Post("https://localhost:5001/CharacterTransforms", JsonUtility.ToJson(new CharacterTransformDto
        {
            positionX = transform.position.x,
            positionY = transform.position.y,
            positionZ = transform.position.z,
            rotationY = transform.rotation.y,
            clientToken = token
        }), "application/json");
        
        request.SetRequestHeader("Authorization", $"Bearer {ServerTokenManager.Instance.Token}");

        yield return request.SendWebRequest();

        //Debug.Log($"SaveTransform result: {request.result}");
        //Debug.Log($"SaveTransform text: {request.downloadHandler.text}");
    }

    private IEnumerator GetTransform()
    {
        using var request = UnityWebRequest.Get("https://localhost:5001/api/CharacterTransforms");

        request.SetRequestHeader("Authorization", $"Bearer {ClientTokenManager.Instance.Token}");

        yield return request.SendWebRequest();

        Debug.Log($"GetTransform result: {request.result}");
        Debug.Log($"GetTransform text: {request.downloadHandler.text}");

        if (request.result == UnityWebRequest.Result.Success)
        {
            var result = JsonUtility.FromJson<CharacterTransformDto>(request.downloadHandler.text);
            transform.position = new Vector3(result.positionX, result.positionY, result.positionZ);
            transform.rotation.Set(0, result.rotationY, 0, 0);
        }
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
}

