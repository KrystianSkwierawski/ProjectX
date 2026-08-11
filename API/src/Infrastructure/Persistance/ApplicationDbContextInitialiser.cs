using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProjectX.Application.CharacterInventories.Queries.GetCharacterInventory;
using ProjectX.Domain.Entities;
using ProjectX.Domain.Enums;
using ProjectX.Infrastructure.Identity;
using Roles = ProjectX.Application.Common.Security.ApplicationRoles;

namespace ProjectX.Infrastructure.Persistance;

public class ApplicationDbContextInitialiser
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<ApplicationDbContextInitialiser> _logger;

    public ApplicationDbContextInitialiser(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ILogger<ApplicationDbContextInitialiser> logger)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    public async Task InitialiseAsync()
    {
        _logger.LogInformation("{Method} -> Start", nameof(InitialiseAsync));

        await _context.Database.EnsureDeletedAsync();
        _logger.LogDebug("{Method} -> Ensured deleted database", nameof(InitialiseAsync));

        await _context.Database.EnsureCreatedAsync();
        _logger.LogDebug("{Method} -> Ensured created database", nameof(InitialiseAsync));

        await InsertOrUpdateQuestsAsync();
        await InsertOrUpdateInventoryItemsAsync();
        await InsertOrUpdateCraftingRecipesAsync();

        await CreateRoleAsync(Roles.Server);
        await CreateRoleAsync(Roles.Client);

        await CreateUserAsync("server1@localhost", "Server1!", Roles.Server, LanguageEnum.en);
        await CreateUserAsync("server2@localhost", "Server2!", Roles.Server, LanguageEnum.pl);
        await CreateUserAsync("user1@localhost", "User1!", Roles.Client, LanguageEnum.en);
        await CreateUserAsync("user2@localhost", "User2!", Roles.Client, LanguageEnum.pl);

        _logger.LogInformation("{Method} -> Stop", nameof(InitialiseAsync));
    }

    #region Helpers

    private async Task CreateRoleAsync(string role)
    {
        if (_roleManager.Roles.All(r => r.Name != role))
        {
            await _roleManager.CreateAsync(new IdentityRole(role));
            _logger.LogDebug("{Method} -> Created role. Name: {Role}", nameof(CreateRoleAsync), role);
        }
    }

    private async Task CreateUserAsync(string userName, string password, string role, LanguageEnum language)
    {
        if (_userManager.Users.All(u => u.UserName != userName))
        {
            var user = new ApplicationUser
            {
                UserName = userName,
                Email = userName,
                Language = language
            };

            await _userManager.CreateAsync(user, password);
            await _userManager.AddToRolesAsync(user, [role]);

            var character = new Character
            {
                ApplicationUserId = user.Id,
                Name = userName.Split('@')[0],
                Status = StatusEnum.Active,
                Health = 100,
                MaxHealth = 100,
                Strength = 1,
                Dexterity = 1,
                Speed = 1,
                Intellect = 1,
                Armor = 1,
                HelmetType = InventoryItemEnum.HelmetTemplate,
                ChestType = InventoryItemEnum.ChestTemplate,
                BootsType = InventoryItemEnum.BootsTemplate,
                WeaponType = InventoryItemEnum.WeaponTemplate,
                AmmoType = InventoryItemEnum.AmmoTemplate,
                CharacterInventory = new CharacterInventory
                {
                    Inventory = JsonSerializer.Serialize(new InventoryDto
                    {
                        Items =
                        [
                            new InventoryItemDto
                            {
                                Type = InventoryItemEnum.HealthPotion,
                                Count = 4
                            },
                            new InventoryItemDto
                            {
                                Type = InventoryItemEnum.Currency,
                                Count = 9999
                            },
                            //new InventoryItemDto
                            //{
                            //    Type = InventoryItemEnum.Currency,
                            //    Count = 50000
                            //},
                            //new InventoryItemDto
                            //{
                            //    Type = InventoryItemEnum.Currency,
                            //    Count = 1000
                            //},
                            //new InventoryItemDto
                            //{
                            //    Type = InventoryItemEnum.Currency,
                            //    Count = 500
                            //}
                        ]
                    }),
                    Count = 15
                }
            };

            character.AddTransform(new CharacterTransform
            {
                PositionX = 3.562874f,
                PositionY = 1.41359f,
                PositionZ = 4.244279f
            });

            _context.Characters.Add(character);

            await _context.SaveChangesAsync();

            _logger.LogDebug(
                "{Method} -> Created user. UserName: {UserName}, Role: {Role}, CharacterId: {CharacterId}",
                nameof(CreateUserAsync),
                userName,
                role,
                character.Id);
        }
    }

    #region TODO: DRY, SR

    private async Task InsertOrUpdateQuestsAsync()
    {
        _logger.LogTrace("{Method} -> Start", nameof(InsertOrUpdateQuestsAsync));

        var dbQuests = await _context.Quests
            .Select(x => new Quest
            {
                Id = x.Id,
                Name = string.Empty,
                GameObjectName = string.Empty
            })
            .ToListAsync();

        _logger.LogDebug("{Method} -> Db quests count: {Count}", nameof(InsertOrUpdateQuestsAsync), dbQuests.Count);

        var enumQuests = Enum.GetValues(typeof(QuestEnum))
            .OfType<QuestEnum>()
            .Where(x => x != QuestEnum.None)
            .ToList();

        _logger.LogDebug("{Method} -> Enum quests count: {Count}", nameof(InsertOrUpdateQuestsAsync), enumQuests.Count);

        var update = enumQuests
            .Where(x => dbQuests.Any(y => y.Id == x))
            .ToDictionary(x => x, x => x.GetParameters())
            .Select(x => new Quest
            {
                Id = x.Key,
                Name = x.Key.ToString(),
                PreviousQuestId = x.Value.PreviousQuestId,
                Type = x.Value.Type,
                GameObjectName = x.Value.GameObjectName,
                Requirement = x.Value.Requirement,
                Reward = x.Value.Reward,
                Status = x.Value.Status
            })
            .ToList();

        _logger.LogDebug("{Method} -> Update quests count: {Count}", nameof(InsertOrUpdateQuestsAsync), update.Count);

        var insert = enumQuests
            .Where(x => !dbQuests.Any(y => y.Id == x))
            .ToDictionary(x => x, x => x.GetParameters())
            .Select(x => new Quest
            {
                Id = x.Key,
                Name = x.Key.ToString(),
                PreviousQuestId = x.Value.PreviousQuestId,
                Type = x.Value.Type,
                GameObjectName = x.Value.GameObjectName,
                Requirement = x.Value.Requirement,
                Reward = x.Value.Reward,
                Status = x.Value.Status
            })
            .ToList();

        _logger.LogDebug("{Method} -> Insert quests count: {Count}", nameof(InsertOrUpdateQuestsAsync), insert.Count);

        var delete = dbQuests
            .Where(x => !update.Any(y => y.Id == x.Id))
            .ToList();

        _logger.LogDebug("{Method} -> Delete quests count: {Count}", nameof(InsertOrUpdateQuestsAsync), delete.Count);

        _context.Quests.UpdateRange(update);
        _context.Quests.AddRange(insert);
        _context.Quests.UpdateRange(delete);

        await _context.SaveChangesAsync();

        _logger.LogTrace("{Method} -> Stop", nameof(InsertOrUpdateQuestsAsync));
    }

    private async Task InsertOrUpdateInventoryItemsAsync()
    {
        _logger.LogTrace("{Method} -> Start", nameof(InsertOrUpdateInventoryItemsAsync));

        var dbInventoryItems = await _context.InventoryItems
            .Select(x => new InventoryItem
            {
                Id = x.Id,
                Name = string.Empty
            })
            .ToListAsync();

        _logger.LogDebug("{Method} -> Db inventory items count: {Count}", nameof(InsertOrUpdateInventoryItemsAsync), dbInventoryItems.Count);

        var enumInventoryItems = Enum.GetValues(typeof(InventoryItemEnum))
            .OfType<InventoryItemEnum>()
            .Where(x => x != InventoryItemEnum.None)
            .ToList();

        _logger.LogDebug("{Method} -> Enum inventory items count: {Count}", nameof(InsertOrUpdateInventoryItemsAsync), enumInventoryItems.Count);

        var update = enumInventoryItems
            .Where(x => dbInventoryItems.Any(y => y.Id == x))
            .Select(x => new InventoryItem
            {
                Id = x,
                Name = x.ToString(),
                MaxCount = byte.MaxValue
            })
            .ToList();

        _logger.LogDebug("{Method} -> Update inventory items count: {Count}", nameof(InsertOrUpdateInventoryItemsAsync), update.Count);

        var insert = enumInventoryItems
            .Where(x => !dbInventoryItems.Any(y => y.Id == x))
            .Select(x => new InventoryItem
            {
                Id = x,
                Name = x.ToString(),
                MaxCount = byte.MaxValue
            })
            .ToList();

        _logger.LogDebug("{Method} -> Insert inventory items count: {Count}", nameof(InsertOrUpdateInventoryItemsAsync), insert.Count);

        var delete = dbInventoryItems
            .Where(x => !update.Any(y => y.Id == x.Id))
            .ToList();

        _logger.LogDebug("{Method} -> Delete inventory items count: {Count}", nameof(InsertOrUpdateInventoryItemsAsync), delete.Count);

        _context.InventoryItems.UpdateRange(update);
        _context.InventoryItems.AddRange(insert);
        _context.InventoryItems.UpdateRange(delete);

        await _context.SaveChangesAsync();

        _logger.LogTrace("{Method} -> Stop", nameof(InsertOrUpdateInventoryItemsAsync));
    }

    private async Task InsertOrUpdateCraftingRecipesAsync()
    {
        _logger.LogTrace("{Method} -> Start", nameof(InsertOrUpdateCraftingRecipesAsync));

        var dbCraftingRecipes = await _context.CraftingRecipes
            .Select(x => new CraftingRecipe
            {
                Id = x.Id,
                Name = string.Empty,
                Requirement = string.Empty,
                Reward = string.Empty
            })
            .ToListAsync();

        _logger.LogDebug("{Method} -> Db crafting recipes count: {Count}", nameof(InsertOrUpdateCraftingRecipesAsync), dbCraftingRecipes.Count);

        var enumCraftingRecipes = Enum.GetValues(typeof(CraftingRecipeEnum))
            .OfType<CraftingRecipeEnum>()
            .Where(x => x != CraftingRecipeEnum.None)
            .ToList();

        _logger.LogDebug("{Method} -> Enum crafting recipes count: {Count}", nameof(InsertOrUpdateCraftingRecipesAsync), enumCraftingRecipes.Count);

        var update = enumCraftingRecipes
            .Where(x => dbCraftingRecipes.Any(y => y.Id == x))
            .ToDictionary(x => x, x => x.GetParameters())
            .Select(x =>
            {
                var name = x.Key.ToString();

                return new CraftingRecipe
                {
                    Id = x.Key,
                    Name = name,
                    Type = x.Value.Type,
                    Requirement = x.Value.Requirement,
                    Reward = x.Value.Reward,
                    Status = x.Value.Status
                };
            })
            .ToList();

        _logger.LogDebug("{Method} -> Update crafting recipes count: {Count}", nameof(InsertOrUpdateCraftingRecipesAsync), update.Count);

        var insert = enumCraftingRecipes
            .Where(x => !dbCraftingRecipes.Any(y => y.Id == x))
            .ToDictionary(x => x, x => x.GetParameters())
            .Select(x =>
            {
                var name = x.Key.ToString();

                return new CraftingRecipe
                {
                    Id = x.Key,
                    Name = name,
                    Type = x.Value.Type,
                    Requirement = x.Value.Requirement,
                    Reward = x.Value.Reward,
                    Status = x.Value.Status
                };
            })
            .ToList();

        _logger.LogDebug("{Method} -> Insert crafting recipes count: {Count}", nameof(InsertOrUpdateCraftingRecipesAsync), insert.Count);

        var delete = dbCraftingRecipes
            .Where(x => !update.Any(y => y.Id == x.Id))
            .ToList();

        _logger.LogDebug("{Method} -> Delete crafting recipes count: {Count}", nameof(InsertOrUpdateCraftingRecipesAsync), delete.Count);

        _context.CraftingRecipes.UpdateRange(update);
        _context.CraftingRecipes.AddRange(insert);
        _context.CraftingRecipes.UpdateRange(delete);

        await _context.SaveChangesAsync();

        _logger.LogTrace("{Method} -> Stop", nameof(InsertOrUpdateCraftingRecipesAsync));
    }

    #endregion

    #endregion
}
