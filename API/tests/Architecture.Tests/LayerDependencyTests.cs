using System.Reflection;
using System.Runtime.CompilerServices;
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
            "Microsoft.EntityFrameworkCore");
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
            "System.IdentityModel.Tokens.Jwt");
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

    private static string FindSolutionDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "ProjectX.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new DirectoryNotFoundException("Could not locate the API solution directory.");
    }
}
