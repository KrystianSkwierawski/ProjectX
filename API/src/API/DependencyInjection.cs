using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using NSwag;
using NSwag.Generation.Processors.Security;
using ProjectX.API.Infrastructure;
using ProjectX.API.Services;
using ProjectX.Application.Common.Interfaces;

namespace ProjectX.API;

public static class DependencyInjection
{
    public static void AddWebServices(this IHostApplicationBuilder builder)
    {
        builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
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
            configure.OperationProcessors.Add(new OpenApiDocumentationOperationProcessor());
            configure.SchemaSettings.SchemaProcessors.Add(new OpenApiSchemaDocumentationProcessor());

            configure.PostProcess = document =>
            {
                document.Security.Clear();
                document.Servers.Clear();
                document.Servers.Add(new OpenApiServer
                {
                    Url = "/",
                    Description = "Current ProjectX API host."
                });

                document.Tags.Clear();
                document.Tags.Add(new OpenApiTag { Name = "ApplicationUsers", Description = "Application-user authentication." });
                document.Tags.Add(new OpenApiTag { Name = "CharacterExperiences", Description = "Character experience and profession progression." });
                document.Tags.Add(new OpenApiTag { Name = "CharacterInventories", Description = "Persistent character inventory state." });
                document.Tags.Add(new OpenApiTag { Name = "CharacterQuests", Description = "Quest lifecycle and character progress." });
                document.Tags.Add(new OpenApiTag { Name = "Characters", Description = "Persistent character state and attributes." });
                document.Tags.Add(new OpenApiTag { Name = "CharacterTransforms", Description = "Persistent character world transforms." });
                document.Tags.Add(new OpenApiTag { Name = "CraftingRecipes", Description = "Available crafting recipes and requirements." });
                document.Tags.Add(new OpenApiTag { Name = "Quests", Description = "Localized quest definitions." });
            };
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
        });
    }
}
