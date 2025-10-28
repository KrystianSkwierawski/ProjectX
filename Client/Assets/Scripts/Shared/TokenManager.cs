using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public sealed class TokenManager : BaseManager<TokenManager>
{
    public string Token { get; private set; }

    public async UniTask LoginAsync(string userName, string password)
    {
        using var request = UnityWebRequest.Post("https://localhost:5001/api/ApplicationUsers", JsonUtility.ToJson(new LoginApplicationUserCommand
        {
            userName = userName,
            password = password
        }), "application/json");

        await request.SendWebRequest();

        Debug.Log($"Login result: {request.result}");

        if (request.result == UnityWebRequest.Result.Success)
        {
            var json = request.downloadHandler.text;
            var result = JsonUtility.FromJson<LoginApplicationUserDto>(json);
            Token = result.token;
            Debug.Log($"Login -> UserName: {userName}, Token: {Token}");
        }
    }
}