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
        Log.Information("InitialiseAsync -> Start");

        await _context.Database.EnsureDeletedAsync();
        Log.Debug("InitialiseAsync -> Ensured deleted database");

        await _context.Database.EnsureCreatedAsync();
        Log.Debug("InitialiseAsync -> Ensured created database");

        await InsertOrUpdateQuestsAsync();
        await InsertOrUpdateCraftingRecipesAsync();

        await CreateRoleAsync(Roles.Server);
        await CreateRoleAsync(Roles.Client);

        await CreateUserAsync("server1@localhost", "Server1!", Roles.Server, LanguageEnum.pl);
        await CreateUserAsync("server2@localhost", "Server2!", Roles.Server, LanguageEnum.en);
        await CreateUserAsync("user1@localhost", "User1!", Roles.Client, LanguageEnum.pl);
        await CreateUserAsync("user2@localhost", "User2!", Roles.Client, LanguageEnum.en);

        Log.Information("InitialiseAsync -> Stop");
    }

    private async Task CreateRoleAsync(string role)
    {
        if (_roleManager.Roles.All(r => r.Name != role))
        {
            await _roleManager.CreateAsync(new IdentityRole(role));
            Log.Debug("CreateRoleAsync -> Created role. Name: {0}", role);
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
                            new InventoryItem
                            {
                                Type = CharacterInventoryTypeEnum.Can,
                                Count = 2
                            }
                        ]
                    }),
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

            Log.Debug("CreateUserAsync -> Created user. UserName: {0}, Role: {1}, CharacterId: {2}", user, role, character.Id);
        }
    }

    #region TODO: DRY

    private async Task InsertOrUpdateQuestsAsync()
    {
        Log.Verbose("InsertOrUpdateQuestsAsync -> Start");

        using var scope = _context.CreateTransactionScope();

        var dbQuests = await _context.Quests
            .Select(x => new Quest
            {
                Id = x.Id
            })
            .ToListAsync();

        Log.Debug("InsertOrUpdateQuestsAsync -> Db quests count: {0}", dbQuests.Count);

        var enumQuests = Enum.GetValues(typeof(QuestEnum))
            .OfType<QuestEnum>()
            .Where(x => x != QuestEnum.None)
            .ToList();

        Log.Debug("InsertOrUpdateQuestsAsync -> Enum quests count: {0}", enumQuests.Count);

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

        Log.Debug("InsertOrUpdateQuestsAsync -> Update quests count: {0}", update.Count);

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

        Log.Debug("InsertOrUpdateQuestsAsync -> Insert quests count: {0}", insert.Count);

        var delete = dbQuests
            .Where(x => !update.Any(y => y.Id == x.Id))
            .ToList();

        Log.Debug("InsertOrUpdateQuestsAsync -> Delete quests count: {0}", delete.Count);

        _context.Quests.UpdateRange(update);
        _context.Quests.AddRange(insert);
        _context.Quests.UpdateRange(delete);

        await _context.SaveChangesAsync();

        scope.Complete();

        Log.Verbose("InsertOrUpdateQuestsAsync -> Stop");
    }

    private async Task InsertOrUpdateCraftingRecipesAsync()
    {
        Log.Verbose("InsertOrUpdateCraftingRecipesAsync -> Start");

        using var scope = _context.CreateTransactionScope();

        var dbCraftingRecipes = await _context.CraftingRecipes
            .Select(x => new CraftingRecipe
            {
                Id = x.Id
            })
            .ToListAsync();

        Log.Debug("InsertOrUpdateCraftingRecipesAsync -> Db crafting recipes count: {0}", dbCraftingRecipes.Count);

        var enumCraftingRecipes = Enum.GetValues(typeof(CraftingRecipeEnum))
            .OfType<CraftingRecipeEnum>()
            .Where(x => x != CraftingRecipeEnum.None)
            .ToList();

        Log.Debug("InsertOrUpdateCraftingRecipesAsync -> Enum crafting recipes count: {0}", enumCraftingRecipes.Count);

        var update = enumCraftingRecipes
            .Where(x => dbCraftingRecipes.Any(y => y.Id == x))
            .ToDictionary(x => x, x => x.GetParameters())
            .Select(x => new CraftingRecipe
            {
                Id = x.Key,
                Name = x.Key.ToString(),
                Requirement = x.Value.Requirement,
                Reward = x.Value.Reward,
                Status = x.Value.Status,
                ModDate = DateTime.Now
            })
            .ToList();

        Log.Debug("InsertOrUpdateCraftingRecipesAsync -> Update crafting recipes count: {0}", update.Count);

        var insert = enumCraftingRecipes
            .Where(x => !dbCraftingRecipes.Any(y => y.Id == x))
            .ToDictionary(x => x, x => x.GetParameters())
            .Select(x => new CraftingRecipe
            {
                Id = x.Key,
                Name = x.Key.ToString(),
                Requirement = x.Value.Requirement,
                Reward = x.Value.Reward,
                Status = x.Value.Status,
                ModDate = DateTime.Now
            })
            .ToList();

        Log.Debug("InsertOrUpdateCraftingRecipesAsync -> Insert crafting recipes count: {0}", insert.Count);

        var delete = dbCraftingRecipes
            .Where(x => !update.Any(y => y.Id == x.Id))
            .ToList();

        Log.Debug("InsertOrUpdateCraftingRecipesAsync -> Delete crafting recipes count: {0}", delete.Count);

        _context.CraftingRecipes.UpdateRange(update);
        _context.CraftingRecipes.AddRange(insert);
        _context.CraftingRecipes.UpdateRange(delete);

        await _context.SaveChangesAsync();

        scope.Complete();

        Log.Verbose("InsertOrUpdateCraftingRecipesAsync -> Stop");
    }

    #endregion
}
