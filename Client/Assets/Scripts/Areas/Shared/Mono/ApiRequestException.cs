using System;
using UnityEngine.Networking;

namespace Assets.Scripts.Areas.Shared.Mono
{
    public sealed class ApiRequestException : Exception
    {
        public ApiRequestException(
            UnityWebRequest.Result result,
            long responseCode,
            string requestError,
            string responseBody)
            : base(requestError)
        {
            Result = result;
            ResponseCode = responseCode;
            ResponseBody = responseBody;
        }

        public UnityWebRequest.Result Result { get; }

        public long ResponseCode { get; }

        public string ResponseBody { get; }
    }
}
