using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public sealed class TokenManager
{
    private static readonly TokenManager _instance = new TokenManager();

    private TokenManager()
    {

    }

    public static TokenManager Instance
    {
        get
        {
            return _instance;
        }
    }

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

    [Serializable]
    private class LoginApplicationUserDto
    {
        public string token;
    }

    [Serializable]
    private class LoginApplicationUserCommand
    {
        public string userName;

        public string password;
    }
}