using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using Assets.Scripts.Areas.Character;
using Assets.Scripts.Areas.Shared.Models;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Assets.Scripts.Areas.Shared.Mono
{
    public static class UnityWebRequestHelper
    {
        private const string _baseUrl = "https://localhost:5001/api"; // FIXME: config/secret
        private const int _requestTimeoutSeconds = 15;

        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public static async UniTask<T> ExecuteGetAsync<T>(
            string endpoint,
            string clientToken = "",
            bool log = true,
            [CallerMemberName] string memberName = "")
        {
            using var request = UnityWebRequest.Get(GetUrl(endpoint));
            request.downloadHandler = new DownloadHandlerBuffer();

            return await SendWebRequestAsync<T>(request, clientToken, log, memberName);
        }

        public static async UniTask<T> ExecutePostAsync<T>(
            string endpoint,
            object body,
            string clientToken = null,
            bool log = true,
            CancellationToken cancellationToken = default,
            [CallerMemberName] string memberName = "")
        {
            var bodyBytes = JsonSerializer.SerializeToUtf8Bytes(body, _jsonOptions);

            using var request = new UnityWebRequest(GetUrl(endpoint), UnityWebRequest.kHttpVerbPOST)
            {
                uploadHandler = new UploadHandlerRaw(bodyBytes) { contentType = "application/json" },
                downloadHandler = new DownloadHandlerBuffer()
            };

            return await SendWebRequestAsync<T>(request, clientToken, log, memberName, cancellationToken);
        }

        public static async UniTask<T> ExecuteDeleteAsync<T>(
            string endpoint,
            string clientToken = null,
            bool log = false,
            [CallerMemberName] string memberName = "")
        {
            using var request = UnityWebRequest.Delete(GetUrl(endpoint));
            request.downloadHandler = new DownloadHandlerBuffer();

            return await SendWebRequestAsync<T>(request, clientToken, log, memberName);
        }

        private static async UniTask<T> SendWebRequestAsync<T>(
            UnityWebRequest request,
            string clientToken,
            bool log = false,
            string memberName = "",
            CancellationToken cancellationToken = default)
        {
            request.timeout = _requestTimeoutSeconds;

            var userToken = UserManager.Instance.Token;

            if (!string.IsNullOrWhiteSpace(userToken))
            {
                request.SetRequestHeader("Authorization", $"Bearer {userToken}");
            }

            if (clientToken != null)
            {
                request.SetRequestHeader("ClientToken", clientToken);
            }

            try
            {
                await request.SendWebRequest().ToUniTask(cancellationToken: cancellationToken);
            }
            catch (UnityWebRequestException) when (!cancellationToken.IsCancellationRequested)
            {
                throw CreateRequestException(request);
            }

            if (log)
            {
                Debug.Log($"{memberName} result: {request.result}");
                Debug.Log($"{memberName} text: {request.downloadHandler?.text}");
            }

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

        private static ApiRequestException CreateRequestException(UnityWebRequest request)
        {
            return new ApiRequestException(
                request.result,
                request.responseCode,
                request.error,
                request.downloadHandler?.text);
        }

        private static string GetUrl(string endpoint)
        {
            return $"{_baseUrl}/{endpoint}";
        }
    }
}
