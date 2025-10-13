using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class TokenManagerService : ITokenManagerService
{
    private const string _url = "https://localhost:5001/api/ApplicationUsers";

    public string Token { get; private set; }

    public async UniTask LoginAsync(string userName, string password)
    {
        using var request = UnityWebRequest.Post(_url, JsonUtility.ToJson(new LoginApplicationUserCommand
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

public interface ITokenManagerService
{
    string Token { get; }

    UniTask LoginAsync(string userName, string password);
}