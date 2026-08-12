using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProjectX.Domain.Entities;
using ProjectX.Domain.Enums;
using ProjectX.Domain.Inventory;
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
        _logger.LogInformation("Starting development database initialization");

        await _context.Database.EnsureDeletedAsync();
        await _context.Database.EnsureCreatedAsync();

        await SeedCatalogsAsync();
        await SeedIdentityAsync();

        _logger.LogInformation("Development database initialization completed");
    }

    #region Development seed data

    private async Task SeedCatalogsAsync()
    {
        _context.Quests.AddRange(Enum.GetValues<QuestEnum>()
            .Where(id => id != QuestEnum.None)
            .Select(id =>
            {
                var parameters = id.GetParameters();

                return new Quest
                {
                    Id = id,
                    Name = id.ToString(),
                    PreviousQuestId = parameters.PreviousQuestId,
                    Type = parameters.Type,
                    GameObjectName = parameters.GameObjectName,
                    Requirement = parameters.Requirement,
                    Reward = parameters.Reward,
                    Status = parameters.Status
                };
            }));

        _context.InventoryItems.AddRange(Enum.GetValues<InventoryItemEnum>()
            .Where(id => id != InventoryItemEnum.None)
            .Select(id => new InventoryItem
            {
                Id = id,
                Name = id.ToString(),
                MaxCount = byte.MaxValue
            }));

        _context.CraftingRecipes.AddRange(Enum.GetValues<CraftingRecipeEnum>()
            .Where(id => id != CraftingRecipeEnum.None)
            .Select(id =>
            {
                var definition = id.GetDefinition();

                return new CraftingRecipe
                {
                    Id = id,
                    Name = id.ToString(),
                    Type = definition.Type,
                    Requirement = definition.Requirement,
                    Reward = definition.Reward,
                    Status = definition.Status
                };
            }));

        await _context.SaveChangesAsync();
    }

    private async Task SeedIdentityAsync()
    {
        await CreateRoleAsync(Roles.Server);
        await CreateRoleAsync(Roles.Client);

        await CreateUserAsync("server1@localhost", "Server1!", Roles.Server, LanguageEnum.en);
        await CreateUserAsync("server2@localhost", "Server2!", Roles.Server, LanguageEnum.pl);
        await CreateUserAsync("user1@localhost", "User1!", Roles.Client, LanguageEnum.en);
        await CreateUserAsync("user2@localhost", "User2!", Roles.Client, LanguageEnum.pl);
    }

    private async Task CreateRoleAsync(string role)
    {
        if (_roleManager.Roles.All(x => x.Name != role))
        {
            await _roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    private async Task CreateUserAsync(string userName, string password, string role, LanguageEnum language)
    {
        if (_userManager.Users.Any(x => x.UserName == userName))
        {
            return;
        }

        var user = new ApplicationUser
        {
            UserName = userName,
            Email = userName,
            Language = language
        };

        await _userManager.CreateAsync(user, password);
        await _userManager.AddToRoleAsync(user, role);

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
                Inventory = new InventoryState(
                [
                    new InventorySlot(InventoryItemEnum.HealthPotion, 4),
                    new InventorySlot(InventoryItemEnum.Currency, 9999)
                ]),
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
            "Created development user {UserName} with role {Role} and character {CharacterId}",
            userName,
            role,
            character.Id);
    }

    #endregion
}
