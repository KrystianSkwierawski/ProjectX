using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using NJsonSchema;
using NJsonSchema.Generation;

namespace ProjectX.API.Infrastructure;

public sealed class OpenApiSchemaDocumentationProcessor : ISchemaProcessor
{
    private static readonly NullabilityInfoContext NullabilityContext = new();

    private static readonly IReadOnlyDictionary<string, string> SchemaDescriptions =
        new Dictionary<string, string>
        {
            ["LoginApplicationUserDto"] = "Successful authentication result containing an access token and preferred language.",
            ["RefreshSessionDto"] = "Successful session-refresh result containing a replacement access token and preferred language.",
            ["LanguageEnum"] = "Supported interface languages.",
            ["HttpValidationProblemDetails"] = "RFC 7807 validation problem containing errors grouped by field.",
            ["ProblemDetails"] = "RFC 7807 machine-readable API error.",
            ["LoginApplicationUserCommand"] = "Credentials used to authenticate an application user.",
            ["RegisterGameSessionCommand"] = "Dedicated-server transport registration. Relay registrations include a Relay join code.",
            ["RegisterGameSessionDto"] = "Identifier and UTC lease expiry assigned to a registered dedicated-server game session.",
            ["HeartbeatGameSessionCommand"] = "Active dedicated-server game session whose lease should be renewed.",
            ["HeartbeatGameSessionDto"] = "Renewed dedicated-server game-session lease and its UTC expiry.",
            ["CreateGameSessionTicketDto"] = "Short-lived, one-time connection ticket and transport details for the active game session.",
            ["RedeemGameSessionTicketCommand"] = "One-time connection ticket presented by the dedicated server during connection approval.",
            ["RedeemGameSessionTicketDto"] = "Server-only player-session credential issued after successful ticket redemption.",
            ["RevokePlayerSessionCommand"] = "Server-only player-session credential to revoke after disconnect.",
            ["AddCharacterExperienceDto"] = "Character experience and calculated level after an experience update.",
            ["AddCharacterExperienceCommand"] = "Experience amount to add to a character progression category.",
            ["ExperienceTypeEnum"] = "Character progression and profession categories.",
            ["CharacterInventoryDto"] = "Persisted character inventory and slot capacity.",
            ["InventoryDto"] = "Ordered collection of inventory slots.",
            ["InventoryItemDto"] = "Inventory item type and stack count.",
            ["InventoryItemEnum"] = "Persisted inventory item identifiers.",
            ["UpdateCharacterInventoryCommand"] = "Inventory additions, removals, stack splits, or slot moves.",
            ["GetCharacterQuestsDto"] = "Quest state assigned to a character.",
            ["CharacterQuestDto"] = "A character's progress and status for one quest.",
            ["QuestEnum"] = "Persisted quest identifiers.",
            ["CharacterQuestStatusEnum"] = "Lifecycle state of a character quest.",
            ["AcceptCharacterQuestCommand"] = "Quest to accept for the authenticated user's character.",
            ["AddCharacterQuestProgressDto"] = "Quest state after progress is added.",
            ["AddCharacterQuestProgressCommand"] = "Progress increment for an accepted character quest.",
            ["CheckCharacterQuestProgressDto"] = "Result of checking character quest progress.",
            ["CheckCharacterQuestProgressCommand"] = "Quest progress reported by the game server.",
            ["CompleteCharacterQuestDto"] = "Reward granted for completing a finished quest.",
            ["CompleteCharacterQuestCommand"] = "Finished character quest to complete.",
            ["CharacterDto"] = "Current character state, attributes, equipment, and progression levels.",
            ["UpdateCharacterCommand"] = "Partial update of character state, attributes, or equipped items.",
            ["CharacterTransformDto"] = "Latest persisted world position and horizontal rotation of a character.",
            ["SaveCharacterTransformCommand"] = "World position and horizontal rotation to persist for a character.",
            ["GetCraftingRecipesDto"] = "Available crafting recipes.",
            ["CraftingRecipeDto"] = "Crafting recipe definition with requirements and reward.",
            ["CraftingRecipeEnum"] = "Persisted crafting recipe identifiers.",
            ["CraftingRecipeRequirementDto"] = "Items and profession level required by a crafting recipe.",
            ["CraftingRecipeRewardDto"] = "Item and experience granted by a crafting recipe.",
            ["CraftingRecipeTypeEnum"] = "Crafting profession used to filter recipes.",
            ["QuestDto"] = "Localized quest definition and completion requirements.",
            ["QuestTypeEnum"] = "Supported quest objective categories.",
            ["GetQuestsDto"] = "Localized quest definitions available to the caller."
        };

    private static readonly IReadOnlyDictionary<string, string> PropertyDescriptions =
        new Dictionary<string, string>
        {
            ["add"] = "Inventory item stacks to add.",
            ["ammoCount"] = "Number of items in the equipped ammunition stack.",
            ["ammoType"] = "Equipped ammunition item type.",
            ["amount"] = "Experience amount to add.",
            ["armor"] = "Character armor attribute value.",
            ["bootsType"] = "Equipped boots item type.",
            ["characterId"] = "Character identifier.",
            ["characterQuestId"] = "Character quest identifier.",
            ["characterQuests"] = "Quest states assigned to the character.",
            ["chestType"] = "Equipped chest item type.",
            ["completeDescription"] = "Localized text shown after completing the quest.",
            ["count"] = "Stack count or inventory slot capacity, depending on the containing schema.",
            ["craftingRecipes"] = "Crafting recipes matching the requested profession.",
            ["description"] = "Localized quest description.",
            ["dexterity"] = "Character dexterity attribute value.",
            ["experience"] = "Experience amount.",
            ["expiresAtUtc"] = "UTC instant after which the credential or game-session lease is no longer valid.",
            ["gameObjectName"] = "Unity GameObject or item name associated with the quest objective.",
            ["gameSessionId"] = "Unique game-session identifier assigned by the API.",
            ["health"] = "Current character health.",
            ["helmetType"] = "Equipped helmet item type.",
            ["id"] = "Resource identifier.",
            ["intellect"] = "Character intellect attribute value.",
            ["inventory"] = "Ordered inventory contents.",
            ["item"] = "Inventory item granted as the crafting reward.",
            ["items"] = "Ordered inventory items or required crafting items.",
            ["language"] = "Preferred interface language.",
            ["level"] = "Calculated character or required profession level.",
            ["levels"] = "Calculated level for each progression category.",
            ["maxHealth"] = "Maximum character health.",
            ["moveSourceSlotIndex"] = "Zero-based source slot index for an inventory move.",
            ["moveTargetSlotIndex"] = "Zero-based target slot index for an inventory move.",
            ["name"] = "Character name.",
            ["password"] = "Application user password.",
            ["positionX"] = "World-space X coordinate.",
            ["positionY"] = "World-space Y coordinate.",
            ["positionZ"] = "World-space Z coordinate.",
            ["playerSessionId"] = "Opaque server-only credential representing an approved player connection.",
            ["previousQuestId"] = "Identifier of the prerequisite quest, or None when there is no prerequisite.",
            ["progress"] = "Quest progress value or increment.",
            ["questId"] = "Quest identifier.",
            ["quests"] = "Localized quest definitions.",
            ["remove"] = "Inventory item stacks to remove.",
            ["requirement"] = "Quest target amount or crafting requirements, depending on the containing schema.",
            ["reward"] = "Quest or crafting reward, depending on the containing schema.",
            ["rotationY"] = "Horizontal world rotation around the Y axis.",
            ["relayJoinCode"] = "Unity Relay allocation join code; present only for Relay sessions.",
            ["speed"] = "Character speed attribute value.",
            ["splitSlotIndex"] = "Zero-based slot index of the stack to split.",
            ["status"] = "Current resource or error status.",
            ["statusText"] = "Localized quest progress text.",
            ["strength"] = "Character strength attribute value.",
            ["title"] = "Localized quest title or problem title.",
            ["token"] = "JWT bearer access token.",
            ["ticket"] = "Random one-time game connection ticket.",
            ["type"] = "Resource type or RFC 7807 problem type, depending on the containing schema.",
            ["usesRelay"] = "Whether the session connects through Unity Relay instead of the local direct transport.",
            ["userName"] = "Application user email address.",
            ["weaponType"] = "Equipped weapon item type."
        };

    public void Process(SchemaProcessorContext context)
    {
        var type = context.ContextualType.Type;

        context.Schema.Description = SchemaDescriptions.GetValueOrDefault(type.Name)
            ?? $"{Humanize(type.Name)} schema.";

        if (type.IsEnum)
        {
            DocumentEnum(context.Schema);
            return;
        }

        foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            var jsonName = property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name
                ?? JsonNamingPolicy.CamelCase.ConvertName(property.Name);

            if (!context.Schema.Properties.TryGetValue(jsonName, out var schemaProperty))
            {
                continue;
            }

            schemaProperty.Description = PropertyDescriptions.GetValueOrDefault(jsonName)
                ?? $"{Humanize(property.Name)} value.";
            schemaProperty.Example = CreateExample(property.PropertyType, jsonName);

            if (IsRequired(property) && !context.Schema.RequiredProperties.Contains(jsonName))
            {
                context.Schema.RequiredProperties.Add(jsonName);
            }

            ApplyLoginConstraints(type, jsonName, schemaProperty);
        }
    }

    private static bool IsRequired(PropertyInfo property)
    {
        if (property.PropertyType.IsValueType)
        {
            return Nullable.GetUnderlyingType(property.PropertyType) is null;
        }

        return NullabilityContext.Create(property).ReadState == NullabilityState.NotNull;
    }

    private static void ApplyLoginConstraints(Type containingType, string propertyName, JsonSchemaProperty schemaProperty)
    {
        if (containingType.Name != "LoginApplicationUserCommand")
        {
            return;
        }

        schemaProperty.MaxLength = 256;

        if (propertyName == "userName")
        {
            schemaProperty.Format = "email";
            schemaProperty.MinLength = 3;
        }
        else if (propertyName == "password")
        {
            schemaProperty.Format = "password";
            schemaProperty.MinLength = 6;
        }
    }

    private static void DocumentEnum(JsonSchema schema)
    {
        schema.EnumerationDescriptions.Clear();

        foreach (var name in schema.EnumerationNames)
        {
            schema.EnumerationDescriptions.Add($"{Humanize(name)}.");
        }
    }

    private static object? CreateExample(Type propertyType, string propertyName)
    {
        var type = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

        if (propertyName == "userName")
        {
            return "user@example.com";
        }

        if (propertyName == "password")
        {
            return "P@ssw0rd!";
        }

        if (propertyName == "token")
        {
            return "eyJhbGciOiJIUzI1NiJ9...";
        }

        if (propertyName == "ticket" || propertyName == "playerSessionId")
        {
            return "base64url-secret";
        }

        if (propertyName == "relayJoinCode")
        {
            return "AB12CD";
        }

        if (type == typeof(Guid))
        {
            return "2f5de532-cf36-48e5-aef6-d31c9f459273";
        }

        if (type == typeof(DateTimeOffset))
        {
            return "2026-08-10T14:01:00Z";
        }

        if (type == typeof(string))
        {
            return "string";
        }

        if (type == typeof(float) || type == typeof(double) || type == typeof(decimal))
        {
            return 0.0;
        }

        if (type == typeof(bool))
        {
            return true;
        }

        if (type.IsPrimitive)
        {
            return 1;
        }

        if (type.IsEnum)
        {
            return Convert.ToInt32(Enum.GetValues(type).GetValue(0));
        }

        if (typeof(System.Collections.IEnumerable).IsAssignableFrom(type))
        {
            return Array.Empty<object>();
        }

        return new Dictionary<string, object>();
    }

    private static string Humanize(string value)
    {
        return string.Concat(value.Select((character, index) =>
            index > 0 && char.IsUpper(character) ? $" {character}" : character.ToString()));
    }
}
