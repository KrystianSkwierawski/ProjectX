using System.Text.Json;

namespace ProjectX.UnitTests.API;

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
    public void LoginEndpoint_ContainsDocumentedResponse(string statusCode)
    {
        using var specification = OpenSpecification();

        var responses = specification.RootElement
            .GetProperty("paths")
            .GetProperty("/api/ApplicationUsers")
            .GetProperty("post")
            .GetProperty("responses");

        Assert.True(responses.TryGetProperty(statusCode, out _));
    }

    [Theory]
    [InlineData("200")]
    [InlineData("401")]
    [InlineData("403")]
    public void RefreshSessionEndpoint_ContainsDocumentedResponse(string statusCode)
    {
        using var specification = OpenSpecification();

        var responses = specification.RootElement
            .GetProperty("paths")
            .GetProperty("/api/ApplicationUsers/RefreshSession")
            .GetProperty("post")
            .GetProperty("responses");

        Assert.True(responses.TryGetProperty(statusCode, out _));
    }

    [Theory]
    [InlineData("/api/GameSessions/Register", "Server")]
    [InlineData("/api/GameSessions/Heartbeat", "Server")]
    [InlineData("/api/GameSessions/Ticket", "Client")]
    [InlineData("/api/GameSessions/Redeem", "Server")]
    [InlineData("/api/GameSessions/RevokePlayer", "Server")]
    public void GameSessionEndpoint_RequiresExpectedAuthorizationPolicy(string path, string expectedPolicy)
    {
        using var specification = OpenSpecification();

        var operation = specification.RootElement
            .GetProperty("paths")
            .GetProperty(path)
            .GetProperty("post");

        Assert.Contains($"Required authorization policy: {expectedPolicy}.", operation.GetProperty("description").GetString());
    }

    [Theory]
    [InlineData("200")]
    [InlineData("404")]
    [InlineData("429")]
    public void GameSessionTicketEndpoint_ContainsDocumentedResponse(string statusCode)
    {
        using var specification = OpenSpecification();

        var responses = specification.RootElement
            .GetProperty("paths")
            .GetProperty("/api/GameSessions/Ticket")
            .GetProperty("post")
            .GetProperty("responses");

        Assert.True(responses.TryGetProperty(statusCode, out _));
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
            .Where(candidate => candidate.Header.ValueKind != JsonValueKind.Undefined)
            .ToArray();

        Assert.Equal(expectedEndpoints.Count, operationsWithPlayerSessionHeader.Length);
        Assert.True(expectedEndpoints.SetEquals(operationsWithPlayerSessionHeader.Select(candidate =>
            (candidate.Operation.Path, candidate.Operation.Method))));

        foreach (var candidate in operationsWithPlayerSessionHeader)
        {
            Assert.True(candidate.Header.GetProperty("required").GetBoolean());
            Assert.False(string.IsNullOrWhiteSpace(candidate.Header.GetProperty("description").GetString()));
            Assert.Equal("string", candidate.Header.GetProperty("schema").GetProperty("type").GetString());
        }
    }

    [Fact]
    public void AllEndpoints_ContainCompleteDocumentation()
    {
        using var specification = OpenSpecification();
        var operations = GetOperations(specification.RootElement).ToArray();

        Assert.Equal(22, operations.Length);

        foreach (var (path, method, operation) in operations)
        {
            Assert.False(string.IsNullOrWhiteSpace(operation.GetProperty("operationId").GetString()), $"{method} {path} has no operationId.");
            Assert.False(string.IsNullOrWhiteSpace(operation.GetProperty("summary").GetString()), $"{method} {path} has no summary.");
            Assert.False(string.IsNullOrWhiteSpace(operation.GetProperty("description").GetString()), $"{method} {path} has no description.");

            if (operation.TryGetProperty("requestBody", out var requestBody))
            {
                Assert.False(string.IsNullOrWhiteSpace(requestBody.GetProperty("description").GetString()), $"{method} {path} has an undocumented request body.");
            }

            if (operation.TryGetProperty("parameters", out var parameters))
            {
                foreach (var parameter in parameters.EnumerateArray())
                {
                    Assert.False(
                        string.IsNullOrWhiteSpace(parameter.GetProperty("description").GetString()),
                        $"{method} {path} parameter {parameter.GetProperty("name").GetString()} has no description.");
                }
            }

            foreach (var response in operation.GetProperty("responses").EnumerateObject())
            {
                Assert.False(
                    string.IsNullOrWhiteSpace(response.Value.GetProperty("description").GetString()),
                    $"{method} {path} response {response.Name} has no description.");
            }
        }
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
    public void Schemas_ContainDescriptionsRequiredPropertiesAndCorrectedNames()
    {
        using var specification = OpenSpecification();
        var schemas = specification.RootElement.GetProperty("components").GetProperty("schemas");

        foreach (var schema in schemas.EnumerateObject())
        {
            Assert.False(string.IsNullOrWhiteSpace(schema.Value.GetProperty("description").GetString()), $"Schema {schema.Name} has no description.");

            if (!schema.Value.TryGetProperty("properties", out var properties))
            {
                continue;
            }

            foreach (var property in properties.EnumerateObject())
            {
                Assert.False(
                    string.IsNullOrWhiteSpace(property.Value.GetProperty("description").GetString()),
                    $"Schema property {schema.Name}.{property.Name} has no description.");
            }

            if (schema.Name is not "ProblemDetails" and not "HttpValidationProblemDetails")
            {
                Assert.True(schema.Value.TryGetProperty("required", out _), $"Schema {schema.Name} has no required properties.");
            }
        }

        var login = schemas.GetProperty("LoginApplicationUserCommand");
        var loginRequired = login.GetProperty("required").EnumerateArray().Select(value => value.GetString()).ToArray();
        Assert.Contains("userName", loginRequired);
        Assert.Contains("password", loginRequired);
        Assert.Equal("email", login.GetProperty("properties").GetProperty("userName").GetProperty("format").GetString());
        Assert.Equal("password", login.GetProperty("properties").GetProperty("password").GetProperty("format").GetString());

        var progressProperties = schemas.GetProperty("AddCharacterQuestProgressCommand").GetProperty("properties");
        Assert.True(progressProperties.TryGetProperty("progress", out _));
        Assert.False(progressProperties.TryGetProperty("progres", out _));

        Assert.True(schemas.TryGetProperty("SaveCharacterTransformCommand", out _));
        Assert.False(schemas.TryGetProperty("SaveTransformTransformCommand", out _));

        var registerSessionRequired = schemas
            .GetProperty("RegisterGameSessionDto")
            .GetProperty("required")
            .EnumerateArray()
            .Select(value => value.GetString())
            .ToArray();
        Assert.Contains("gameSessionId", registerSessionRequired);
        Assert.Contains("expiresAtUtc", registerSessionRequired);

        var heartbeatRequired = schemas
            .GetProperty("HeartbeatGameSessionDto")
            .GetProperty("required")
            .EnumerateArray()
            .Select(value => value.GetString())
            .ToArray();
        Assert.Contains("gameSessionId", heartbeatRequired);
        Assert.Contains("expiresAtUtc", heartbeatRequired);
    }

    [Fact]
    public void DocumentMetadata_ContainsInfoServerTagsAndStableOperationIds()
    {
        using var specification = OpenSpecification();
        var root = specification.RootElement;
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

        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("info").GetProperty("description").GetString()));
        Assert.True(root.GetProperty("servers").GetArrayLength() > 0);
        Assert.Equal(9, root.GetProperty("tags").GetArrayLength());

        var tags = root.GetProperty("tags")
            .EnumerateArray()
            .Select(tag => tag.GetProperty("name").GetString())
            .ToHashSet();

        Assert.Contains("GameSessions", tags);

        var actualOperationIds = GetOperations(root)
            .Select(operation => operation.Operation.GetProperty("operationId").GetString()!)
            .ToHashSet();

        Assert.True(expectedOperationIds.SetEquals(actualOperationIds));
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
