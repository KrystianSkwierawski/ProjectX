using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class BaseTokenManager : MonoBehaviour
{
    public string Token { get; private set; }

    public string UserName;

    public string Password;

    protected IEnumerator Login()
    {
        using var request = UnityWebRequest.Post("https://localhost:5001/api/ApplicationUsers", JsonUtility.ToJson(new LoginApplicationUserCommand
        {
            userName = UserName,
            password = Password
        }), "application/json");

        yield return request.SendWebRequest();

        Debug.Log($"Login result: {request.result}");

        if (request.result == UnityWebRequest.Result.Success)
        {
            var json = request.downloadHandler.text;
            var result = JsonUtility.FromJson<LoginApplicationUserDto>(json);
            Token = result.token;
            Debug.Log($"Login Token: {Token}");
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