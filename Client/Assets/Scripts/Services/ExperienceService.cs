using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using static ExperienceService;

public interface IExperienceService
{
    UniTask<AddCharacterExperienceDto> AddExperienceAsync(string token);
}

public class ExperienceService : IExperienceService
{
    private readonly ITokenManagerService _tokenManagerService;

    public ExperienceService(ITokenManagerService tokenManagerService)
    {
        _tokenManagerService = tokenManagerService;
    }

    public async UniTask<AddCharacterExperienceDto> AddExperienceAsync(string token)
    {
        using var request = UnityWebRequest.Post("https://localhost:5001/api/CharacterExperiences", JsonUtility.ToJson(new AddCharacterExperienceCommand
        {
            amount = 1000,
            type = ExperienceTypeEnum.Combat,
            clientToken = token
        }), "application/json");

        request.SetRequestHeader("Authorization", $"Bearer {_tokenManagerService.Token}");

        await request.SendWebRequest();

        Debug.Log($"AddExperience result: {request.result}");
        Debug.Log($"AddExperience text: {request.downloadHandler.text}");

        if (request.result == UnityWebRequest.Result.Success)
        {
            return JsonUtility.FromJson<AddCharacterExperienceDto>(request.downloadHandler.text);
        }

        throw new Exception(request.error);
    }

    [Serializable]
    private class AddCharacterExperienceCommand
    {
        public int amount;

        public ExperienceTypeEnum type;

        public string clientToken;
    }

    [Serializable]
    public class AddCharacterExperienceDto
    {
        public byte level;

        public byte skillPoints;

        public int experience;

        public bool leveledUp;
    }

    private enum ExperienceTypeEnum : byte
    {
        None,

        Combat,

        Crafting,

        Gathering,

        Exploration,

        Questing,

        Trading,

        Survival,

        Technology,

        Healing,

        Building,

        Farming,

        Fishing,

        Cooking,

        Alchemy,

        Enchanting,
    }
}
