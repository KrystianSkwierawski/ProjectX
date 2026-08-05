using Microsoft.AspNetCore.Diagnostics;
using ProjectX.Application.Common.Exceptions;

namespace ProjectX.API.Infrastructure;

public sealed class ApiExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var result = exception switch
        {
            ValidationException validationException => Results.ValidationProblem(
                validationException.Errors,
                statusCode: StatusCodes.Status400BadRequest,
                title: "Request validation failed"),
            NotFoundException notFoundException => Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Resource not found",
                detail: notFoundException.Message),
            _ => null
        };

        if (result is null)
        {
            return false;
        }

        await result.ExecuteAsync(httpContext);

        return true;
    }
}
