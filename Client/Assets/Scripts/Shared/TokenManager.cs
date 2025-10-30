using Assets.Scripts.Models;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Shared
{
    public class TokenManager : Singleton<TokenManager>
    {
        public string Token { get; private set; }

        public async UniTask LoginAsync(string userName, string password)
        {
            var result = await UnityWebRequestHelper.ExecutePostAsync<LoginApplicationUserDto>("ApplicationUsers", new LoginApplicationUserCommand
            {
                userName = userName,
                password = password
            });

            Token = result.token;

            Debug.Log($"Login -> UserName: {userName}, Token: {Token}");
        }
    }
}