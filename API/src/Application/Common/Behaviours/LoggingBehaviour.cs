using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using ProjectX.Application.Common.Exceptions;
using ProjectX.Application.Common.Interfaces;

namespace ProjectX.Application.Common.Behaviours;

public class LoggingBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<LoggingBehaviour<TRequest, TResponse>> _logger;

    public LoggingBehaviour(
        ICurrentUserService currentUserService,
        ILogger<LoggingBehaviour<TRequest, TResponse>> logger)
    {
        _currentUserService = currentUserService;
        _logger = logger;
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

            _logger.LogDebug(
                "{RequestName} -> Start. UserId: {UserId}, Request: {Request}",
                requestName,
                userId,
                request.ToString());

            var stopwatch = Stopwatch.StartNew();
            var response = await next();
            stopwatch.Stop();

            _logger.LogDebug(
                "{RequestName} -> Stop. UserId: {UserId}, Elapsed: {Elapsed}, Response: {Response}",
                requestName,
                userId,
                stopwatch.Elapsed,
                response?.ToString());

            return response;
        }
        catch (Exception exception) when (exception is InvalidCredentialsException or InvalidGameSessionCredentialException)
        {
            _logger.LogDebug("{RequestName} -> Rejected credentials", requestName);
            throw;
        }
        catch (Exception exception) when (exception is ValidationException or NotFoundException)
        {
            _logger.LogDebug("{RequestName} -> Rejected: {Reason}", requestName, exception.Message);
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "{RequestName} failed", requestName);
            throw;
        }
    }
}
