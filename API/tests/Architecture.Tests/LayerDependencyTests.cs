using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Xml.Linq;
using ProjectX.Application.ApplicationUsers.Commands.LoginApplicationUser;
using ProjectX.Domain.Entities;
using ProjectX.Infrastructure.Persistance;

namespace ProjectX.Architecture.Tests;

public class LayerDependencyTests
{
    private static readonly string SolutionDirectory = FindSolutionDirectory();

    [Fact]
    public void Domain_DoesNotReferenceOuterLayersOrFrameworks()
    {
        AssertDoesNotReference(
            typeof(Character).Assembly,
            "ProjectX.Application",
            "ProjectX.Infrastructure",
            "ProjectX.API",
            "Microsoft.AspNetCore",
            "Microsoft.EntityFrameworkCore",
            "System.Text.Json");
    }

    [Fact]
    public void Domain_TypesUseDomainNamespace()
    {
        AssertTypesUseNamespace(typeof(Character).Assembly, "ProjectX.Domain");
    }

    [Fact]
    public void Application_DoesNotReferenceInfrastructurePresentationOrAdapterLibraries()
    {
        AssertDoesNotReference(
            typeof(LoginApplicationUserCommand).Assembly,
            "ProjectX.Infrastructure",
            "ProjectX.API",
            "Microsoft.AspNetCore.Identity",
            "Serilog",
            "Newtonsoft.Json",
            "System.IdentityModel.Tokens.Jwt",
            "System.Text.Json");
    }

    [Fact]
    public void Infrastructure_DoesNotReferencePresentation()
    {
        AssertDoesNotReference(typeof(ApplicationDbContext).Assembly, "ProjectX.API");
    }

    [Fact]
    public void Application_TypesUseApplicationNamespace()
    {
        AssertTypesUseNamespace(typeof(LoginApplicationUserCommand).Assembly, "ProjectX.Application");
    }

    [Fact]
    public void Infrastructure_TypesUseInfrastructureNamespace()
    {
        AssertTypesUseNamespace(typeof(ApplicationDbContext).Assembly, "ProjectX.Infrastructure");
    }

    [Fact]
    public void ProductionProjects_FollowAllowedProjectReferenceGraph()
    {
        AssertProjectReferences("Domain");
        AssertProjectReferences("Application", "Domain");
        AssertProjectReferences("Infrastructure", "Application");
        AssertProjectReferences("API", "Application", "Infrastructure");
    }

    [Fact]
    public void TestProjects_ReferenceOnlyTheLayersTheyExercise()
    {
        AssertTestProjectReferences("Domain.UnitTests", "Domain");
        AssertTestProjectReferences("Application.UnitTests", "Application");
        AssertTestProjectReferences("Infrastructure.IntegrationTests", "Infrastructure");
        AssertTestProjectReferences("Web.AcceptanceTests", "API");
        AssertTestProjectReferences("Architecture.Tests", "Application", "Domain", "Infrastructure");
    }

    [Fact]
    public void Domain_DoesNotDeclarePackageDependencies()
    {
        var packageReferences = GetItemReferences("Domain", "PackageReference");

        Assert.True(
            packageReferences.Length == 0,
            $"Domain declares package dependencies: {string.Join(", ", packageReferences)}");
    }

    [Fact]
    public void Application_DoesNotDeclareFrameworkAdapterPackages()
    {
        var forbiddenPrefixes = new[]
        {
            "Microsoft.AspNetCore.Authentication",
            "Microsoft.AspNetCore.Identity",
            "Microsoft.IdentityModel",
            "Newtonsoft.Json",
            "NSwag",
            "Serilog",
            "System.IdentityModel.Tokens.Jwt"
        };

        var forbiddenReferences = GetItemReferences("Application", "PackageReference")
            .Where(reference => forbiddenPrefixes.Any(prefix => reference.StartsWith(prefix, StringComparison.Ordinal)))
            .ToArray();

        Assert.True(
            forbiddenReferences.Length == 0,
            $"Application declares framework adapter packages: {string.Join(", ", forbiddenReferences)}");
    }

    [Fact]
    public void BuildConfiguration_UsesNet10WithPinnedStableSdk()
    {
        var buildProperties = XDocument.Load(Path.Combine(SolutionDirectory, "Directory.Build.props"));
        var targetFramework = GetPropertyValue(buildProperties, "TargetFramework");
        var warningsAsErrors = GetPropertyValue(buildProperties, "TreatWarningsAsErrors");

        using var globalJson = JsonDocument.Parse(File.ReadAllText(Path.Combine(SolutionDirectory, "global.json")));
        var sdk = globalJson.RootElement.GetProperty("sdk");

        Assert.Equal("net10.0", targetFramework);
        Assert.Equal("true", warningsAsErrors);
        Assert.StartsWith("10.0.", sdk.GetProperty("version").GetString());
        Assert.Equal("latestFeature", sdk.GetProperty("rollForward").GetString());
        Assert.False(sdk.GetProperty("allowPrerelease").GetBoolean());

        using var toolManifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            SolutionDirectory,
            ".config",
            "dotnet-tools.json")));

        Assert.Equal(
            "10.0.11",
            toolManifest.RootElement
                .GetProperty("tools")
                .GetProperty("dotnet-ef")
                .GetProperty("version")
                .GetString());
    }

    [Fact]
    public void Solution_UsesSlnxAndContainsEveryBackendProject()
    {
        var solutionPath = Path.Combine(SolutionDirectory, "ProjectX.slnx");
        var actualProjects = XDocument.Load(solutionPath)
            .Descendants("Project")
            .Select(element => element.Attribute("Path")?.Value)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Cast<string>()
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        var expectedProjects = new[]
        {
            "src/API/API.csproj",
            "src/Application/Application.csproj",
            "src/Domain/Domain.csproj",
            "src/Infrastructure/Infrastructure.csproj",
            "tests/Application.UnitTests/Application.UnitTests.csproj",
            "tests/Architecture.Tests/Architecture.Tests.csproj",
            "tests/Domain.UnitTests/Domain.UnitTests.csproj",
            "tests/Infrastructure.IntegrationTests/Infrastructure.IntegrationTests.csproj",
            "tests/Web.AcceptanceTests/Web.AcceptanceTests.csproj"
        };

        Assert.Equal(expectedProjects, actualProjects);
        Assert.False(File.Exists(Path.Combine(SolutionDirectory, "ProjectX.sln")));
    }

    [Fact]
    public void ApiConfiguration_KeepsDevelopmentSettingsAndSecretsOutOfBaseConfiguration()
    {
        using var baseConfiguration = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            SolutionDirectory,
            "src",
            "API",
            "appsettings.json")));
        using var developmentConfiguration = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            SolutionDirectory,
            "src",
            "API",
            "appsettings.Development.json")));

        var root = baseConfiguration.RootElement;
        var jwtSettings = root.GetProperty("JwtSettings");

        Assert.False(root.TryGetProperty("UseInMemoryDatabase", out _));
        Assert.False(root.TryGetProperty("API", out _));
        Assert.False(jwtSettings.TryGetProperty("SecurityKey", out _));
        Assert.True(developmentConfiguration.RootElement.GetProperty("UseInMemoryDatabase").GetBoolean());

        var apiProject = XDocument.Load(Path.Combine(SolutionDirectory, "src", "API", "API.csproj"));

        Assert.False(string.IsNullOrWhiteSpace(GetPropertyValue(apiProject, "UserSecretsId")));
        Assert.DoesNotContain("JwtSettings__SecurityKey=", apiProject.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void EditorConfig_FollowsCleanArchitectureFormattingConventions()
    {
        var editorConfig = File.ReadAllText(Path.Combine(SolutionDirectory, ".editorconfig"));

        Assert.Contains("end_of_line = lf", editorConfig, StringComparison.Ordinal);
        Assert.Contains("dotnet_sort_system_directives_first = true", editorConfig, StringComparison.Ordinal);
        Assert.Contains("csharp_style_namespace_declarations = file_scoped", editorConfig, StringComparison.Ordinal);
        Assert.Contains("csharp_new_line_before_open_brace = all", editorConfig, StringComparison.Ordinal);
    }

    private static void AssertDoesNotReference(Assembly assembly, params string[] forbiddenPrefixes)
    {
        var forbiddenReferences = assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .Where(reference => forbiddenPrefixes.Any(prefix => reference.StartsWith(prefix, StringComparison.Ordinal)))
            .OrderBy(reference => reference)
            .ToArray();

        Assert.True(
            forbiddenReferences.Length == 0,
            $"{assembly.GetName().Name} references forbidden assemblies: {string.Join(", ", forbiddenReferences)}");
    }

    private static void AssertTypesUseNamespace(Assembly assembly, string expectedNamespace)
    {
        var misplacedTypes = assembly
            .GetTypes()
            .Where(type => !IsCompilerGenerated(type))
            .Where(type => type.Namespace is null || !type.Namespace.StartsWith(expectedNamespace, StringComparison.Ordinal))
            .Select(type => type.FullName ?? type.Name)
            .OrderBy(type => type)
            .ToArray();

        Assert.True(
            misplacedTypes.Length == 0,
            $"{assembly.GetName().Name} types outside {expectedNamespace} namespace: {string.Join(", ", misplacedTypes)}");
    }

    private static bool IsCompilerGenerated(Type type)
    {
        return type.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false)
            || type.DeclaringType is not null && IsCompilerGenerated(type.DeclaringType);
    }

    private static void AssertProjectReferences(string projectName, params string[] expectedReferences)
    {
        AssertProjectReferencesIn("src", projectName, expectedReferences);
    }

    private static void AssertTestProjectReferences(string projectName, params string[] expectedReferences)
    {
        AssertProjectReferencesIn("tests", projectName, expectedReferences);
    }

    private static void AssertProjectReferencesIn(string projectRoot, string projectName, params string[] expectedReferences)
    {
        var actualReferences = GetItemReferences(projectRoot, projectName, "ProjectReference")
            .Select(Path.GetFileNameWithoutExtension)
            .OrderBy(reference => reference, StringComparer.Ordinal)
            .ToArray();

        var expected = expectedReferences
            .OrderBy(reference => reference, StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            actualReferences.SequenceEqual(expected, StringComparer.Ordinal),
            $"{projectName} project references [{string.Join(", ", actualReferences)}], expected [{string.Join(", ", expected)}].");
    }

    private static string[] GetItemReferences(string projectName, string itemName)
    {
        return GetItemReferences("src", projectName, itemName);
    }

    private static string[] GetItemReferences(string projectRoot, string projectName, string itemName)
    {
        var projectPath = Path.Combine(SolutionDirectory, projectRoot, projectName, $"{projectName}.csproj");

        return XDocument.Load(projectPath)
            .Descendants()
            .Where(element => element.Name.LocalName == itemName)
            .Select(element => element.Attribute("Include")?.Value)
            .Where(reference => !string.IsNullOrWhiteSpace(reference))
            .Cast<string>()
            .OrderBy(reference => reference, StringComparer.Ordinal)
            .ToArray();
    }

    private static string? GetPropertyValue(XDocument document, string propertyName)
    {
        return document
            .Descendants()
            .Single(element => element.Name.LocalName == propertyName)
            .Value;
    }

    private static string FindSolutionDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "ProjectX.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new DirectoryNotFoundException("Could not locate the API solution directory.");
    }
}
