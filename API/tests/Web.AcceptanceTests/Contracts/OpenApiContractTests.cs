using System.Text.Json;

namespace ProjectX.Web.AcceptanceTests.Contracts;

public class OpenApiContractTests
{
    private static readonly HashSet<string> HttpMethods =
    [
        "delete",
        "get",
        "patch",
        "post",
        "put"
    ];

    [Theory]
    [InlineData("200")]
    [InlineData("400")]
    [InlineData("401")]
    [InlineData("429")]
    public void LoginEndpoint_ContainsExpectedResponse(string statusCode)
    {
        using var specification = OpenSpecification();

        Assert.True(GetOperation(specification.RootElement, "/api/ApplicationUsers", "post")
            .GetProperty("responses")
            .TryGetProperty(statusCode, out _));
    }

    [Theory]
    [InlineData("200")]
    [InlineData("404")]
    [InlineData("429")]
    public void GameSessionTicketEndpoint_ContainsExpectedResponse(string statusCode)
    {
        using var specification = OpenSpecification();

        Assert.True(GetOperation(specification.RootElement, "/api/GameSessions/Ticket", "post")
            .GetProperty("responses")
            .TryGetProperty(statusCode, out _));
    }

    [Fact]
    public void ServerPlayerSessionEndpoints_RequirePlayerSessionIdHeader()
    {
        using var specification = OpenSpecification();

        var expectedEndpoints = new HashSet<(string Path, string Method)>
        {
            ("/api/CharacterExperiences", "POST"),
            ("/api/CharacterInventories", "POST"),
            ("/api/CharacterQuests/Progress", "POST"),
            ("/api/CharacterQuests/CheckProgress", "POST"),
            ("/api/CharacterQuests/Complete", "POST"),
            ("/api/Characters/{id}", "GET"),
            ("/api/Characters", "POST"),
            ("/api/CharacterTransforms", "POST")
        };

        var operationsWithPlayerSessionHeader = GetOperations(specification.RootElement)
            .Select(operation => new
            {
                Operation = operation,
                Header = GetParameters(operation.Operation)
                    .SingleOrDefault(parameter =>
                        parameter.GetProperty("in").GetString() == "header"
                        && parameter.GetProperty("name").GetString() == "PlayerSessionId")
            })
            .Where(x => x.Header.ValueKind != JsonValueKind.Undefined)
            .ToArray();

        Assert.Equal(expectedEndpoints.Count, operationsWithPlayerSessionHeader.Length);
        Assert.True(expectedEndpoints.SetEquals(operationsWithPlayerSessionHeader.Select(x =>
            (x.Operation.Path, x.Operation.Method))));

        foreach (var operationWithHeader in operationsWithPlayerSessionHeader)
        {
            Assert.True(operationWithHeader.Header.GetProperty("required").GetBoolean());
            Assert.Equal("string", operationWithHeader.Header.GetProperty("schema").GetProperty("type").GetString());
        }
    }

    [Fact]
    public void Endpoints_HaveStableNamesAndConciseDescriptions()
    {
        using var specification = OpenSpecification();

        var operations = GetOperations(specification.RootElement).ToArray();

        Assert.Equal(22, operations.Length);

        foreach (var (path, method, operation) in operations)
        {
            Assert.False(string.IsNullOrWhiteSpace(operation.GetProperty("operationId").GetString()), $"{method} {path} has no operationId.");
            Assert.False(string.IsNullOrWhiteSpace(operation.GetProperty("summary").GetString()), $"{method} {path} has no summary.");
            Assert.False(string.IsNullOrWhiteSpace(operation.GetProperty("description").GetString()), $"{method} {path} has no description.");
        }

        var expectedOperationIds = new HashSet<string>
        {
            "AcceptCharacterQuest",
            "AddCharacterExperience",
            "AddCharacterQuestProgress",
            "CheckCharacterQuestProgress",
            "CompleteCharacterQuest",
            "GetCharacter",
            "GetCharacterInventory",
            "GetCharacterQuests",
            "GetCharacterTransform",
            "GetCraftingRecipes",
            "GetQuest",
            "GetQuests",
            "CreateTicketAsync",
            "HeartbeatAsync",
            "LoginAsync",
            "RedeemTicketAsync",
            "RegisterAsync",
            "RefreshSessionAsync",
            "RevokePlayerAsync",
            "SaveCharacterTransform",
            "UpdateCharacter",
            "UpdateCharacterInventory"
        };

        Assert.True(expectedOperationIds.SetEquals(operations.Select(operation =>
            operation.Operation.GetProperty("operationId").GetString()!)));
    }

    [Fact]
    public void AuthenticationContract_UsesBearerJwtAndKeepsLoginAnonymous()
    {
        using var specification = OpenSpecification();

        var root = specification.RootElement;
        var jwt = root.GetProperty("components").GetProperty("securitySchemes").GetProperty("JWT");

        Assert.Equal("http", jwt.GetProperty("type").GetString());
        Assert.Equal("bearer", jwt.GetProperty("scheme").GetString());
        Assert.Equal("JWT", jwt.GetProperty("bearerFormat").GetString());
        Assert.False(root.TryGetProperty("security", out _));

        var operations = GetOperations(root).ToArray();
        var login = Assert.Single(operations, operation => operation.Path == "/api/ApplicationUsers");

        Assert.False(login.Operation.TryGetProperty("security", out _));

        foreach (var operation in operations.Where(operation => operation.Path != "/api/ApplicationUsers"))
        {
            Assert.True(operation.Operation.TryGetProperty("security", out _), $"{operation.Method} {operation.Path} has no JWT requirement.");

            var responses = operation.Operation.GetProperty("responses");
            Assert.True(responses.TryGetProperty("401", out _), $"{operation.Method} {operation.Path} has no 401 response.");
            Assert.True(responses.TryGetProperty("403", out _), $"{operation.Method} {operation.Path} has no 403 response.");
        }
    }

    [Fact]
    public void Schemas_ExposeRequiredLoginFieldsAndCurrentContractNames()
    {
        using var specification = OpenSpecification();

        var schemas = specification.RootElement.GetProperty("components").GetProperty("schemas");
        var loginRequired = schemas
            .GetProperty("LoginApplicationUserCommand")
            .GetProperty("required")
            .EnumerateArray()
            .Select(value => value.GetString())
            .ToArray();

        Assert.Contains("userName", loginRequired);
        Assert.Contains("password", loginRequired);

        var progressProperties = schemas.GetProperty("AddCharacterQuestProgressCommand").GetProperty("properties");
        Assert.True(progressProperties.TryGetProperty("progress", out _));
        Assert.False(progressProperties.TryGetProperty("progres", out _));

        Assert.True(schemas.TryGetProperty("SaveCharacterTransformCommand", out _));
        Assert.False(schemas.TryGetProperty("SaveTransformTransformCommand", out _));
    }

    private static JsonElement GetOperation(JsonElement root, string path, string method)
    {
        return root
            .GetProperty("paths")
            .GetProperty(path)
            .GetProperty(method);
    }

    private static JsonDocument OpenSpecification()
    {
        var specificationPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "..",
            "src",
            "API",
            "wwwroot",
            "api",
            "specification.json"));

        return JsonDocument.Parse(File.ReadAllText(specificationPath));
    }

    private static IEnumerable<(string Path, string Method, JsonElement Operation)> GetOperations(JsonElement root)
    {
        foreach (var path in root.GetProperty("paths").EnumerateObject())
        {
            foreach (var method in path.Value.EnumerateObject())
            {
                if (HttpMethods.Contains(method.Name))
                {
                    yield return (path.Name, method.Name.ToUpperInvariant(), method.Value);
                }
            }
        }
    }

    private static IEnumerable<JsonElement> GetParameters(JsonElement operation)
    {
        return operation.TryGetProperty("parameters", out var parameters)
            ? parameters.EnumerateArray()
            : [];
    }
}
