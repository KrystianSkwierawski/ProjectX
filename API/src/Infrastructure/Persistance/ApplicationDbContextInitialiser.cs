using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProjectX.Application.CharacterInventories.Queries.GetCharacterInventory;
using ProjectX.Domain.Constants;
using ProjectX.Domain.Entities;
using ProjectX.Domain.Enums;

namespace ProjectX.Infrastructure.Persistance;
public static class InitialiserExtensions
{
    public static async Task InitialiseDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<ApplicationDbContextInitialiser>();

        await service.InitialiseAsync();
    }
}

public class ApplicationDbContextInitialiser
{
    private static readonly Serilog.ILogger Log = Serilog.Log.ForContext<ApplicationDbContextInitialiser>();

    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public ApplicationDbContextInitialiser(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task InitialiseAsync()
    {
        Log.Information("{0} -> Start", nameof(InitialiseAsync));

        await _context.Database.EnsureDeletedAsync();
        Log.Debug("{0} -> Ensured deleted database", nameof(InitialiseAsync));

        await _context.Database.EnsureCreatedAsync();
        Log.Debug("{0} -> Ensured created database", nameof(InitialiseAsync));

        await InsertOrUpdateQuestsAsync();
        await InsertOrUpdateInventoryItemsAsync();
        await InsertOrUpdateCraftingRecipesAsync();

        await CreateRoleAsync(Roles.Server);
        await CreateRoleAsync(Roles.Client);

        await CreateUserAsync("server1@localhost", "Server1!", Roles.Server, LanguageEnum.pl);
        await CreateUserAsync("server2@localhost", "Server2!", Roles.Server, LanguageEnum.en);
        await CreateUserAsync("user1@localhost", "User1!", Roles.Client, LanguageEnum.pl);
        await CreateUserAsync("user2@localhost", "User2!", Roles.Client, LanguageEnum.en);

        Log.Information("{0} -> Stop", nameof(InitialiseAsync));
    }

    #region Helpers

    private async Task CreateRoleAsync(string role)
    {
        if (_roleManager.Roles.All(r => r.Name != role))
        {
            await _roleManager.CreateAsync(new IdentityRole(role));
            Log.Debug("{0} -> Created role. Name: {1}", nameof(CreateRoleAsync), role);
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
                ModDate = DateTime.Now,
                CharacterInventory = new CharacterInventory
                {
                    Inventory = JsonSerializer.Serialize(new InventoryDto
                    {
                        Items =
                        [
                            new InventoryItemDto
                            {
                                Type = InventoryItemEnum.Can,
                                Count = 2
                            },
                            new InventoryItemDto
                            {
                                Type = InventoryItemEnum.Rice,
                                Count = 2
                            },
                            new InventoryItemDto
                            {
                                Type = InventoryItemEnum.Fish,
                                Count = 2
                            },
                            new InventoryItemDto
                            {
                                Type = InventoryItemEnum.PurpleOre,
                                Count = 2
                            },
                            new InventoryItemDto
                            {
                                Type = InventoryItemEnum.WhiteOre,
                                Count = 2
                            },
                            new InventoryItemDto
                            {
                                Type = InventoryItemEnum.CopperOre,
                                Count = 2
                            },
                            new InventoryItemDto
                            {
                                Type = InventoryItemEnum.BlackOre,
                                Count = 2
                            },
                            new InventoryItemDto
                            {
                                Type = InventoryItemEnum.Chamomile,
                                Count = 2
                            },
                        ]
                    }),
                    ModDate = DateTime.Now,
                    Count = 15
                },
                CharacterTransforms =
                [
                    new CharacterTransform
                    {
                        PositionX = 3.562874f,
                        PositionY = 1.41359f,
                        PositionZ = 4.244279f,
                        ModDate = DateTime.Now
                    }
                ]
            };

            _context.Characters.Add(character);

            await _context.SaveChangesAsync();

            Log.Debug("{0} -> Created user. UserName: {1}, Role: {2}, CharacterId: {3}", nameof(CreateUserAsync), user, role, character.Id);
        }
    }

    #region TODO: DRY, SR

    private async Task InsertOrUpdateQuestsAsync()
    {
        Log.Verbose("{0} -> Start", nameof(InsertOrUpdateQuestsAsync));

        var dbQuests = await _context.Quests
            .Select(x => new Quest
            {
                Id = x.Id
            })
            .ToListAsync();

        Log.Debug("{0} -> Db quests count: {1}", nameof(InsertOrUpdateQuestsAsync), dbQuests.Count);

        var enumQuests = Enum.GetValues(typeof(QuestEnum))
            .OfType<QuestEnum>()
            .Where(x => x != QuestEnum.None)
            .ToList();

        Log.Debug("{0} -> Enum quests count: {1}", nameof(InsertOrUpdateQuestsAsync), enumQuests.Count);

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
                Status = x.Value.Status,
                ModDate = DateTime.Now
            })
            .ToList();

        Log.Debug("{0} -> Update quests count: {1}", nameof(InsertOrUpdateQuestsAsync), update.Count);

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
                Status = x.Value.Status,
                ModDate = DateTime.Now
            })
            .ToList();

        Log.Debug("{0} -> Insert quests count: {1}", nameof(InsertOrUpdateQuestsAsync), insert.Count);

        var delete = dbQuests
            .Where(x => !update.Any(y => y.Id == x.Id))
            .ToList();

        Log.Debug("{0} -> Delete quests count: {1}", nameof(InsertOrUpdateQuestsAsync), delete.Count);

        _context.Quests.UpdateRange(update);
        _context.Quests.AddRange(insert);
        _context.Quests.UpdateRange(delete);

        await _context.SaveChangesAsync();

        Log.Verbose("{0} -> Stop", nameof(InsertOrUpdateQuestsAsync));
    }

    private async Task InsertOrUpdateInventoryItemsAsync()
    {
        Log.Verbose("{0} -> Start", nameof(InsertOrUpdateInventoryItemsAsync));

        var dbInventoryItems = await _context.InventoryItems
            .Select(x => new InventoryItem
            {
                Id = x.Id
            })
            .ToListAsync();

        Log.Debug("{0} -> Db inventory items count: {1}", nameof(InsertOrUpdateInventoryItemsAsync), dbInventoryItems.Count);

        var enumInventoryItems = Enum.GetValues(typeof(InventoryItemEnum))
            .OfType<InventoryItemEnum>()
            .Where(x => x != InventoryItemEnum.None)
            .ToList();

        Log.Debug("{0} -> Enum inventory items count: {1}", nameof(InsertOrUpdateInventoryItemsAsync), enumInventoryItems.Count);

        var update = enumInventoryItems
            .Where(x => dbInventoryItems.Any(y => y.Id == x))
            .Select(x => new InventoryItem
            {
                Id = x,
                Name = x.ToString(),
                MaxCount = byte.MaxValue,
                ModDate = DateTime.Now
            })
            .ToList();

        Log.Debug("{0} -> Update inventory items count: {1}", nameof(InsertOrUpdateInventoryItemsAsync), update.Count);

        var insert = enumInventoryItems
            .Where(x => !dbInventoryItems.Any(y => y.Id == x))
            .Select(x => new InventoryItem
            {
                Id = x,
                Name = x.ToString(),
                MaxCount = byte.MaxValue,
                ModDate = DateTime.Now
            })
            .ToList();

        Log.Debug("{0} -> Insert inventory items count: {1}", nameof(InsertOrUpdateInventoryItemsAsync), insert.Count);

        var delete = dbInventoryItems
            .Where(x => !update.Any(y => y.Id == x.Id))
            .ToList();

        Log.Debug("{0} -> Delete inventory items count: {1}", nameof(InsertOrUpdateInventoryItemsAsync), delete.Count);

        _context.InventoryItems.UpdateRange(update);
        _context.InventoryItems.AddRange(insert);
        _context.InventoryItems.UpdateRange(delete);

        await _context.SaveChangesAsync();

        Log.Verbose("{0} -> Stop", nameof(InsertOrUpdateInventoryItemsAsync));
    }

    private async Task InsertOrUpdateCraftingRecipesAsync()
    {
        Log.Verbose("{0} -> Start", nameof(InsertOrUpdateCraftingRecipesAsync));

        var dbCraftingRecipes = await _context.CraftingRecipes
            .Select(x => new CraftingRecipe
            {
                Id = x.Id
            })
            .ToListAsync();

        Log.Debug("{0} -> Db crafting recipes count: {1}", nameof(InsertOrUpdateCraftingRecipesAsync), dbCraftingRecipes.Count);

        var enumCraftingRecipes = Enum.GetValues(typeof(CraftingRecipeEnum))
            .OfType<CraftingRecipeEnum>()
            .Where(x => x != CraftingRecipeEnum.None)
            .ToList();

        Log.Debug("{0} -> Enum crafting recipes count: {1}", nameof(InsertOrUpdateCraftingRecipesAsync), enumCraftingRecipes.Count);

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
                    Status = x.Value.Status,
                    ModDate = DateTime.Now
                };
            })
            .ToList();

        Log.Debug("{0} -> Update crafting recipes count: {1}", nameof(InsertOrUpdateCraftingRecipesAsync), update.Count);

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
                    Status = x.Value.Status,
                    ModDate = DateTime.Now
                };
            })
            .ToList();

        Log.Debug("{0} -> Insert crafting recipes count: {1}", nameof(InsertOrUpdateCraftingRecipesAsync), insert.Count);

        var delete = dbCraftingRecipes
            .Where(x => !update.Any(y => y.Id == x.Id))
            .ToList();

        Log.Debug("{0} -> Delete crafting recipes count: {1}", nameof(InsertOrUpdateCraftingRecipesAsync), delete.Count);

        _context.CraftingRecipes.UpdateRange(update);
        _context.CraftingRecipes.AddRange(insert);
        _context.CraftingRecipes.UpdateRange(delete);

        await _context.SaveChangesAsync();

        Log.Verbose("{0} -> Stop", nameof(InsertOrUpdateCraftingRecipesAsync));
    }

    #endregion

    #endregion
}
