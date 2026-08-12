using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NSwag;
using NSwag.Generation.Processors.Security;
using ProjectX.API.Infrastructure;
using ProjectX.API.Services;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Application.Common.Security;

namespace ProjectX.API;

public static class DependencyInjection
{
    public static void AddWebServices(this IHostApplicationBuilder builder)
    {
        builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
        builder.Services.AddSingleton<IAuthorizationHandler, PlayerSessionAuthorizationHandler>();
        builder.Services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
            options.AddPolicy(AuthorizationPolicies.Server, policy => policy.RequireRole(ApplicationRoles.Server));
            options.AddPolicy(AuthorizationPolicies.Client, policy => policy.RequireRole(ApplicationRoles.Client));
            options.AddPolicy(AuthorizationPolicies.ServerOrClient, policy => policy.RequireRole(ApplicationRoles.Server, ApplicationRoles.Client));
            options.AddPolicy(AuthorizationPolicies.ServerPlayerSession, policy =>
            {
                policy.RequireRole(ApplicationRoles.Server);
                policy.AddRequirements(new PlayerSessionAuthorizationRequirement());
            });
        });
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddExceptionHandler<ApiExceptionHandler>();
        builder.Services.AddProblemDetails();
        builder.Services.Configure<ApiBehaviorOptions>(options => options.SuppressModelStateInvalidFilter = true);

        builder.Services.AddEndpointsApiExplorer();

        builder.Services.AddOpenApiDocument((configure, sp) =>
        {
            configure.Title = "ProjectX.API";
            configure.Version = "1.0.0";
            configure.Description = "HTTP API used by the ProjectX Unity client and dedicated game server for authentication and persistent game state.";

            configure.AddSecurity("JWT", Enumerable.Empty<string>(), new OpenApiSecurityScheme
            {
                Type = OpenApiSecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "JWT bearer token returned by the application-user login endpoint."
            });

            configure.OperationProcessors.Add(new AspNetCoreOperationSecurityScopeProcessor("JWT"));
            configure.OperationProcessors.Add(new OpenApiOperationProcessor());

            configure.PostProcess = document => document.Security.Clear();
        });

        builder.Services.AddMemoryCache();

        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(RateLimitPolicies.Login, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        AutoReplenishment = true
                    }));

            options.AddPolicy(RateLimitPolicies.GameSessionTicket, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    context.User.FindFirstValue(ClaimTypes.NameIdentifier) is { Length: > 0 } userId
                        ? $"user:{userId}"
                        : $"ip:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 20,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        AutoReplenishment = true
                    }));
        });
    }
}
