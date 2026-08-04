using System;
using System.Collections.Generic;
using System.Threading;
using Assets.Scripts.Areas.Character.Enums;
using Assets.Scripts.Areas.Character.Models;
using Assets.Scripts.Areas.Professions.Enums;
using Assets.Scripts.Areas.Shared.Enums;
using Assets.Scripts.Areas.Shared.Mono;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

namespace Assets.Scripts.Areas.Character
{
    public class UserManager : Singleton<UserManager>
    {
        public IDictionary<ulong, CharacterDto> Characters { get; } = new Dictionary<ulong, CharacterDto>();

        public string Token { get; private set; }

        public LanguageEnum Language { get; private set; }

        public ulong OwnerClientId { get; set; } // TODO: replace all references

        public async UniTask LoginAsync(string userName, string password, CancellationToken cancellationToken = default)
        {
            var result = await UnityWebRequestHelper.ExecutePostAsync<LoginApplicationUserDto>("ApplicationUsers", new LoginApplicationUserCommand
            {
                UserName = userName,
                Password = password
            }, log: false, cancellationToken: cancellationToken);

            if (string.IsNullOrWhiteSpace(result?.Token))
            {
                throw new InvalidOperationException("The login response did not contain an access token.");
            }

            Token = result.Token;
            Language = result.Language;

            Debug.Log($"Login -> UserName: {userName}, Language: {Language}");
        }

        public byte GetLevelByRecipeType(CraftingRecipeTypeEnum craftingRecipeType)
        {
            return GetLevelByRecipeType(craftingRecipeType, NetworkManager.Singleton.LocalClientId);
        }

        public byte GetLevelByRecipeType(CraftingRecipeTypeEnum craftingRecipeType, ulong clientId)
        {
            var type = craftingRecipeType switch
            {
                CraftingRecipeTypeEnum.Cooking => ExperienceTypeEnum.Cooking,
                CraftingRecipeTypeEnum.Blacksmithing => ExperienceTypeEnum.Blacksmithing,
                CraftingRecipeTypeEnum.Alchemy => ExperienceTypeEnum.Alchemy,
                _ => ExperienceTypeEnum.None,
            };

            if (type == ExperienceTypeEnum.None)
            {
                return 0;
            }

            return Characters[clientId].Levels[type];
        }
    }
}
