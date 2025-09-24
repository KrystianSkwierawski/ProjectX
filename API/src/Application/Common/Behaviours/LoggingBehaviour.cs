using System.Diagnostics;
using MediatR;
using ProjectX.Application.Common.Interfaces;

namespace ProjectX.Application.Common.Behaviours;

public class LoggingBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly Serilog.ILogger Log = Serilog.Log.ForContext(typeof(LoggingBehaviour<,>));

    private readonly ICurrentUserService _user;

    public LoggingBehaviour(ICurrentUserService user)
    {
        _user = user;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        try
        {
            Log.Debug("{0} -> Start. UserId: {1}, Request: {2}", requestName, _user.Id, request);

            var sw = Stopwatch.StartNew();

            var response = await next();

            sw.Stop();

            Log.Debug("{0} -> Stop. UserId: {1}, Elapsed: {2}, Response: {3}", requestName, _user.Id, sw.Elapsed, response);

            return response;
        }
        catch (Exception ex)
        {
            Log.Error(ex, ex.Message);
            throw;
        }
    }
}
