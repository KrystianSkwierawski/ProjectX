using System;
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
        private const string _developmentBaseUrl = "https://localhost:5001/api";
        internal const int RequestTimeoutSeconds = 15;

        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public static async UniTask<T> ExecuteGetAsync<T>(string endpoint, string playerSessionId = null, bool log = true,
            CancellationToken cancellationToken = default, [CallerMemberName] string memberName = "")
        {
            using var request = UnityWebRequest.Get(GetUrl(endpoint));
            request.downloadHandler = new DownloadHandlerBuffer();

            return await SendWebRequestAsync<T>(request, playerSessionId, log, memberName, cancellationToken);
        }

        public static async UniTask<T> ExecutePostAsync<T>(string endpoint, object body, string playerSessionId = null, bool log = true,
            CancellationToken cancellationToken = default, [CallerMemberName] string memberName = "")
        {
            var bodyBytes = JsonSerializer.SerializeToUtf8Bytes(body, _jsonOptions);

            using var request = new UnityWebRequest(GetUrl(endpoint), UnityWebRequest.kHttpVerbPOST)
            {
                uploadHandler = new UploadHandlerRaw(bodyBytes) { contentType = "application/json" },
                downloadHandler = new DownloadHandlerBuffer()
            };

            return await SendWebRequestAsync<T>(request, playerSessionId, log, memberName, cancellationToken);
        }

        public static async UniTask<T> ExecutePostAsync<T>(string endpoint, bool log = true, CancellationToken cancellationToken = default,
            [CallerMemberName] string memberName = "")
        {
            using var request = new UnityWebRequest(GetUrl(endpoint), UnityWebRequest.kHttpVerbPOST)
            {
                downloadHandler = new DownloadHandlerBuffer()
            };

            return await SendWebRequestAsync<T>(request, null, log, memberName, cancellationToken);
        }

        public static async UniTask<T> ExecuteDeleteAsync<T>(string endpoint, string playerSessionId = null, bool log = false,
            CancellationToken cancellationToken = default, [CallerMemberName] string memberName = "")
        {
            using var request = UnityWebRequest.Delete(GetUrl(endpoint));
            request.downloadHandler = new DownloadHandlerBuffer();

            return await SendWebRequestAsync<T>(request, playerSessionId, log, memberName, cancellationToken);
        }

        private static async UniTask<T> SendWebRequestAsync<T>(UnityWebRequest request, string playerSessionId, bool log = false,
            string memberName = "", CancellationToken cancellationToken = default)
        {
            request.timeout = RequestTimeoutSeconds;

            var userToken = UserManager.Instance.Token;

            if (!string.IsNullOrWhiteSpace(userToken))
            {
                request.SetRequestHeader("Authorization", $"Bearer {userToken}");
            }

            if (!string.IsNullOrWhiteSpace(playerSessionId))
            {
                request.SetRequestHeader("PlayerSessionId", playerSessionId);
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
            return new ApiRequestException(request.result, request.responseCode, request.error, request.downloadHandler?.text);
        }

        private static string GetUrl(string endpoint)
        {
            return $"{GetBaseUrl()}/{endpoint.TrimStart('/')}";
        }

        private static string GetBaseUrl()
        {
            var configuredUrl = Environment.GetEnvironmentVariable("PROJECTX_API_URL");

            if (string.IsNullOrWhiteSpace(configuredUrl))
            {
                configuredUrl = GetCommandLineValue("-projectx-api-url");
            }

            if (string.IsNullOrWhiteSpace(configuredUrl)
                && (Application.isEditor
                    || IsEnvironmentFlagEnabled("PROJECTX_USE_DIRECT_TRANSPORT")
                    || HasCommandLineSwitch("-projectx-direct")))
            {
                configuredUrl = _developmentBaseUrl;
            }

            if (string.IsNullOrWhiteSpace(configuredUrl))
            {
                throw new InvalidOperationException("PROJECTX_API_URL or -projectx-api-url is required outside local development.");
            }

            configuredUrl = configuredUrl.Trim().TrimEnd('/');

            if (!Uri.TryCreate(configuredUrl, UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttps && (uri.Scheme != Uri.UriSchemeHttp || !uri.IsLoopback)))
            {
                throw new InvalidOperationException("The ProjectX API URL must be an absolute HTTPS URL (HTTP is allowed only for loopback development).");
            }

            return configuredUrl;
        }

        private static bool HasCommandLineSwitch(string value)
        {
            foreach (var argument in Environment.GetCommandLineArgs())
            {
                if (string.Equals(argument, value, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsEnvironmentFlagEnabled(string name)
        {
            var value = Environment.GetEnvironmentVariable(name);

            return string.Equals(value, "1", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetCommandLineValue(string name)
        {
            var arguments = Environment.GetCommandLineArgs();

            for (var index = 0; index < arguments.Length - 1; index++)
            {
                if (string.Equals(arguments[index], name, StringComparison.OrdinalIgnoreCase))
                {
                    return arguments[index + 1];
                }
            }

            return null;
        }
    }
}
