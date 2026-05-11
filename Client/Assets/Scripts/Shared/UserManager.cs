using Assets.Scripts.Enums;
using Assets.Scripts.Models;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Shared
{
    public class UserManager : Singleton<UserManager>
    {
        public string Token { get; private set; }

        public LanguageEnum Language { get; private set; }

        public ulong OwnerClientId { get; set; } // TODO: replace all references

        public async UniTask LoginAsync(string userName, string password)
        {
            var result = await UnityWebRequestHelper.ExecutePostAsync<LoginApplicationUserDto>("ApplicationUsers", new LoginApplicationUserCommand
            {
                UserName = userName,
                Password = password
            });

            Token = result.Token;
            Language = result.Language;

            Debug.Log($"Login -> UserName: {userName}, Token: {Token}, Language: {Language}");
        }
    }
}