using ProjectX.Application.CharacterSettings.Commands.UpdateCharacterSettings;
using ProjectX.Domain.Enums;

namespace ProjectX.Application.UnitTests.CharacterSettings;

public class UpdateCharacterSettingsValidatorTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(9)]
    [InlineData(11)]
    public void RejectsWrongSlotCount(int length)
    {
        Assert.False(new UpdateCharacterSettingsValidator().Validate(new UpdateCharacterSettingsCommand(42, LanguageEnum.en, new InventoryItemEnum[length])).IsValid);
    }

    [Fact]
    public void RejectsNullSlotsAndInvalidLanguage()
    {
        var validator = new UpdateCharacterSettingsValidator();

        Assert.False(validator.Validate(new UpdateCharacterSettingsCommand(42, LanguageEnum.en, null!)).IsValid);
        Assert.False(validator.Validate(new UpdateCharacterSettingsCommand(42, (LanguageEnum)99, new InventoryItemEnum[10])).IsValid);
    }

    [Theory]
    [InlineData(InventoryItemEnum.Can)]
    [InlineData(InventoryItemEnum.HelmetTemplate)]
    [InlineData((InventoryItemEnum)9999)]
    public void RejectsNonUsableItems(InventoryItemEnum type)
    {
        var slots = new InventoryItemEnum[10];

        slots[3] = type;

        Assert.False(new UpdateCharacterSettingsValidator().Validate(new UpdateCharacterSettingsCommand(42, LanguageEnum.en, slots)).IsValid);
    }

    [Theory]
    [InlineData(InventoryItemEnum.Currency)]
    [InlineData(InventoryItemEnum.None)]
    [InlineData(InventoryItemEnum.HealthPotion)]
    [InlineData(InventoryItemEnum.StrengthPotion)]
    [InlineData(InventoryItemEnum.SpeedPotion)]
    [InlineData(InventoryItemEnum.IronBow)]
    [InlineData(InventoryItemEnum.AmmoOil3)]
    public void AcceptsUsableItemsAndEmptySlots(InventoryItemEnum type)
    {
        var slots = new InventoryItemEnum[10];

        slots[9] = type;

        Assert.True(new UpdateCharacterSettingsValidator().Validate(new UpdateCharacterSettingsCommand(42, LanguageEnum.en, slots)).IsValid);
    }
}
