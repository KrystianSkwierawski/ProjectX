using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Networking;

public class Player : NetworkBehaviour
{
    [SerializeField] private UIManager _uiManager;

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

            UIManager.Instance.SetPlayer(result.name, result.health.ToString(), result.level.ToString());
        }
    }
}
