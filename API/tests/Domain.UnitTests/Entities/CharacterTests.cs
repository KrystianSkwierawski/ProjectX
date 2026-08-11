using ProjectX.Domain.Characters;
using ProjectX.Domain.Entities;

namespace ProjectX.Domain.UnitTests.Entities;

public class CharacterTests
{
    [Fact]
    public void UpdateState_AppliesOnlyValuesReportedByTheGameServer()
    {
        var character = new Character
        {
            ApplicationUserId = "user-id",
            Name = "Character",
            Health = 120,
            MaxHealth = 120,
            Strength = 10
        };

        character.UpdateState(new CharacterStateUpdate(
            Health: null,
            MaxHealth: 100,
            Strength: 12,
            Dexterity: null,
            Speed: null,
            Intellect: null,
            Armor: null,
            HelmetType: null,
            ChestType: null,
            BootsType: null,
            WeaponType: null,
            AmmoType: null,
            AmmoCount: null));

        Assert.Equal(120, character.Health);
        Assert.Equal(100, character.MaxHealth);
        Assert.Equal(12, character.Strength);
    }
}
