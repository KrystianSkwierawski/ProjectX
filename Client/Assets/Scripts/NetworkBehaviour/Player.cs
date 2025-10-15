using Cysharp.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Networking;

public class Player : NetworkBehaviour
{
    private async void Start()
    {
        if (IsOwner)
        {
            await GetCharacterAsync();
        }
    }

    private async UniTask GetCharacterAsync()
    {
        using var request = UnityWebRequest.Get("https://localhost:5001/api/Characters/1");

        request.SetRequestHeader("Authorization", $"Bearer {TokenManager.Instance.Token}");

        await request.SendWebRequest();

        Debug.Log($"GetCharacter result: {request.result}");
        Debug.Log($"GetCharacter text: {request.downloadHandler.text}");

        if (request.result == UnityWebRequest.Result.Success)
        {
            var result = JsonUtility.FromJson<CharacterDto>(request.downloadHandler.text);

            var playerCanvas = GameObject.Find("PlayerCanvas");

            playerCanvas.transform.Find("Player/Name").GetComponent<TextMeshProUGUI>().text = result.name;
            playerCanvas.transform.Find("Player/HealthPoints").GetComponent<TextMeshProUGUI>().text = result.health.ToString();
            playerCanvas.transform.Find("Player/Level").GetComponent<TextMeshProUGUI>().text = $"Level: {result.level}";
        }
    }

    private class CharacterDto
    {
        public string name;

        public byte level;

        public byte skillPoints;

        public int health;
    }
}
