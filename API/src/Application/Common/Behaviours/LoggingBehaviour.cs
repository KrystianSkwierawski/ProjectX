using System.Diagnostics;
using MediatR;
using ProjectX.Application.Common.Exceptions;
using ProjectX.Application.Common.Interfaces;

namespace ProjectX.Application.Common.Behaviours;

public class LoggingBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly Serilog.ILogger Log = Serilog.Log.ForContext(typeof(LoggingBehaviour<,>));

    private readonly ICurrentUserService _currentUserService;

    public LoggingBehaviour(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        try
        {
            var userId = _currentUserService.GetId();

            Log.Debug(
                "{RequestName} -> Start. UserId: {UserId}, Request: {Request}",
                requestName,
                userId,
                GetLogPayload(request));

            var sw = Stopwatch.StartNew();

            var response = await next();

            sw.Stop();

            Log.Debug(
                "{RequestName} -> Stop. UserId: {UserId}, Elapsed: {Elapsed}, Response: {Response}",
                requestName,
                userId,
                sw.Elapsed,
                GetLogPayload(response));

            return response;
        }
        catch (InvalidCredentialsException)
        {
            Log.Debug("{RequestName} -> Rejected credentials", requestName);
            throw;
        }
        catch (Exception exception) when (exception is ValidationException or NotFoundException)
        {
            Log.Debug("{RequestName} -> Rejected: {Reason}", requestName, exception.Message);
            throw;
        }
        catch (Exception exception)
        {
            Log.Error(exception, exception.Message);
            throw;
        }
    }

    private static string? GetLogPayload<TPayload>(TPayload payload)
    {
        return payload?.ToString();
    }
}
