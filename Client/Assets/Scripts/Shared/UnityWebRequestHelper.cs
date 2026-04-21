using System;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Assets.Scripts.Models;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Assets.Scripts.Shared
{
    public static class UnityWebRequestHelper
    {
        private static readonly string _baseUrl = "https://localhost:5001/api";

        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public static async UniTask<T> ExecuteGetAsync<T>(string endpoint, string clientToken = "", bool log = true, [CallerMemberName] string memberName = "")
        {
            using var request = UnityWebRequest.Get($"{_baseUrl}/{endpoint}");

            request.downloadHandler = new DownloadHandlerBuffer();

            return await SendWebRequestAsync<T>(request, clientToken, log, memberName);
        }

        public static async UniTask<T> ExecutePostAsync<T>(string endpoint, object obj, string clientToken = null, bool log = true, [CallerMemberName] string memberName = "")
        {
            var bodyBytes = JsonSerializer.SerializeToUtf8Bytes(obj, _jsonOptions);

            using var request = new UnityWebRequest($"{_baseUrl}/{endpoint}", UnityWebRequest.kHttpVerbPOST)
            {
                uploadHandler = new UploadHandlerRaw(bodyBytes) { contentType = "application/json" },
                downloadHandler = new DownloadHandlerBuffer()
            };

            return await SendWebRequestAsync<T>(request, clientToken, log, memberName);
        }

        public static async UniTask<T> ExecuteDeleteAsync<T>(string endpoint, string clientToken = null, bool log = false, [CallerMemberName] string memberName = "")
        {
            using var request = UnityWebRequest.Delete($"{_baseUrl}/{endpoint}");

            request.downloadHandler = new DownloadHandlerBuffer();

            return await SendWebRequestAsync<T>(request, clientToken, log, memberName);
        }

        private static async UniTask<T> SendWebRequestAsync<T>(UnityWebRequest request, string clientToken, bool log = false, string memberName = "")
        {
            request.SetRequestHeader("Authorization", $"Bearer {UserManager.Instance.Token}");

            if (clientToken != null)
            {
                request.SetRequestHeader("ClientToken", clientToken);
            }

            await request.SendWebRequest();

            if (log)
            {
                Debug.Log($"{memberName} result: {request.result}");
                Debug.Log($"{memberName} text: {request.downloadHandler?.text}");
            }

            if (request.result == UnityWebRequest.Result.Success)
            {
                if (EmptyResponse.Instance is T empty)
                {
                    return empty;
                }

                if ((request.downloadHandler?.data?.Length ?? 0) == 0)
                {
                    return default;
                }

                return JsonSerializer.Deserialize<T>(request.downloadHandler.data, _jsonOptions);
            }

            throw new Exception(request.error);
        }
    }
}