using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using ProjectX.API.Infrastructure;
using ProjectX.Application.Common.Exceptions;

namespace ProjectX.UnitTests.API;

public class ApiExceptionHandlerTests
{
    [Theory]
    [MemberData(nameof(HandledExceptions))]
    public async Task TryHandleAsync_MapsApplicationExceptionToExpectedStatusCode(Exception exception, int expectedStatusCode)
    {
        await using var services = new ServiceCollection()
            .AddLogging()
            .AddProblemDetails()
            .BuildServiceProvider();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = services,
            Response = { Body = new MemoryStream() }
        };
        var handler = new ApiExceptionHandler();

        var handled = await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(expectedStatusCode, httpContext.Response.StatusCode);
    }

    [Fact]
    public async Task TryHandleAsync_ReturnsFalseForUnknownException()
    {
        var httpContext = new DefaultHttpContext();
        var handler = new ApiExceptionHandler();

        var handled = await handler.TryHandleAsync(httpContext, new InvalidOperationException(), CancellationToken.None);

        Assert.False(handled);
    }

    public static TheoryData<Exception, int> HandledExceptions => new()
    {
        { new InvalidCredentialsException(), StatusCodes.Status401Unauthorized },
        { new InvalidGameSessionCredentialException(), StatusCodes.Status401Unauthorized },
        { new ForbiddenAccessException(), StatusCodes.Status403Forbidden },
        { new ValidationException(), StatusCodes.Status400BadRequest },
        { new NotFoundException("resource"), StatusCodes.Status404NotFound }
    };
}
