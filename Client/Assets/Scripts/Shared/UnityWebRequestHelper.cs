using System;
using System.Runtime.CompilerServices;
using Assets.Scripts.Models;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Assets.Scripts.Shared
{
    public static class UnityWebRequestHelper
    {
        private static readonly string _baseUrl = "https://localhost:5001/api";

        public static async UniTask<T> ExecuteGetAsync<T>(string endpoint, string clientToken = "", [CallerMemberName] string memberName = "")
        {
            using var request = UnityWebRequest.Get($"{_baseUrl}/{endpoint}");

            return await SendWebRequestAsync<T>(request, clientToken, memberName);
        }

        public static async UniTask<T> ExecutePostAsync<T>(string endpoint, object obj, string clientToken = null, [CallerMemberName] string memberName = "")
        {
            var data = JsonUtility.ToJson(obj);

            using var request = UnityWebRequest.Post($"{_baseUrl}/{endpoint}", data, "application/json");

            return await SendWebRequestAsync<T>(request, clientToken, memberName);
        }

        public static async UniTask<T> ExecutePutAsync<T>(string endpoint, object obj, string clientToken = null, [CallerMemberName] string memberName = "")
        {
            var data = JsonUtility.ToJson(obj);

            using var request = UnityWebRequest.Put($"{_baseUrl}/{endpoint}", data);

            return await SendWebRequestAsync<T>(request, clientToken, memberName);
        }

        private static async UniTask<T> SendWebRequestAsync<T>(UnityWebRequest request, string clientToken, string memberName)
        {
            request.SetRequestHeader("Authorization", $"Bearer {TokenManager.Instance.Token}");

            if (clientToken != null)
            {
                request.SetRequestHeader("ClientToken", clientToken);
            }

            await request.SendWebRequest();

            Debug.Log($"{memberName} result: {request.result}");
            Debug.Log($"{memberName} text: {request.downloadHandler.text}");

            if (request.result == UnityWebRequest.Result.Success)
            {
                if (EmptyResponse.Instance is T empty)
                {
                    return empty;
                }

                return JsonUtility.FromJson<T>(request.downloadHandler.text);
            }

            throw new Exception(request.error);
        }
    }
}