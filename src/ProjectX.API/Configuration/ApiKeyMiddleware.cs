
namespace ProjectX.API.Configuration;

public class ApiKeyMiddleware : IMiddleware
{
    private static readonly Serilog.ILogger Log = Serilog.Log.ForContext(typeof(ApiKeyMiddleware));

    private readonly IAppConfiguration _configuration;

    public ApiKeyMiddleware(IAppConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (_configuration.SwaggerEnabled && context.Request.Path.StartsWithSegments("/swagger"))
        {
            await next(context);
            return;
        }

        if (_configuration.ApiKey != context.Request.Headers["X-Api-Key"])
        {
            Log.Warning("Invalid API key provided.");
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        await next(context);
    }
}
