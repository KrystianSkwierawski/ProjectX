using Cysharp.Threading.Tasks;
using UnityEngine;
using Assets.Scripts.Areas.Character.Models;
using Assets.Scripts.Areas.Shared.Enums;
using Assets.Scripts.Areas.Shared.Mono;

namespace Assets.Scripts.Areas.Character
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