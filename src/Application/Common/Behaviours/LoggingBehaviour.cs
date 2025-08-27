using System.Diagnostics;
using MediatR;
using ProjectX.Application.Common.Interfaces;

namespace ProjectX.Application.Common.Behaviours;

public class LoggingBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly Serilog.ILogger Log = Serilog.Log.ForContext(typeof(LoggingBehaviour<,>));

    private readonly IUser _user;

    public LoggingBehaviour(IUser user)
    {
        _user = user;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        try
        {
            Log.Debug("{0} -> Start. Request: {1}, UserId: {2}", requestName, request, _user.Id);

            var sw = Stopwatch.StartNew();

            var response = await next();

            sw.Stop();

            Log.Debug("{0} -> Stop. Response: {1}, Elapsed: {2}, UserId: {3}", requestName, response, sw.Elapsed, _user.Id);

            return response;
        }
        catch (Exception ex)
        {
            Log.Error(ex, ex.Message);
            throw;
        }
    }
}
