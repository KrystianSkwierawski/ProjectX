using MediatR;
using ProjectX.Application.ApplicationUsers.Commands.LoginApplicationUser;

namespace ProjectX.Application.UnitTests.Common;

public class RequestLoggingConventionTests
{
    [Fact]
    public void AllRequests_OverrideToString()
    {
        var requestTypesWithoutToString = typeof(LoginApplicationUserCommand).Assembly
            .GetTypes()
            .Where(type => !type.IsAbstract)
            .Where(type => typeof(IBaseRequest).IsAssignableFrom(type))
            .Where(type => type.GetMethod(nameof(ToString), Type.EmptyTypes)?.DeclaringType != type)
            .Select(type => type.FullName)
            .OrderBy(typeName => typeName)
            .ToArray();

        Assert.Empty(requestTypesWithoutToString);
    }
}
