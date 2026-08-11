using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using ProjectX.Application.Common;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Domain.Constants;
using ProjectX.Domain.Entities;
using ProjectX.Infrastructure.GameSessions;
using ProjectX.Infrastructure.Persistance;
using ProjectX.Infrastructure.Persistance.Interceptors;


namespace ProjectX.Infrastructure;

public static class DependencyInjection
{
    public static void AddInfrastructureServices(this IHostApplicationBuilder builder)
    {
        var timeProvider = TimeProvider.System;

        builder.Services.AddSingleton<TimeProvider>(timeProvider);

        var ticketLifetimeSeconds = builder.Configuration.GetValue<int?>("GameSessionSettings:TicketLifetimeSeconds") ?? 60;

        if (ticketLifetimeSeconds is < 10 or > 300)
        {
            throw new InvalidOperationException("GameSessionSettings:TicketLifetimeSeconds must be between 10 and 300 seconds.");
        }

        var serverLeaseSeconds = builder.Configuration.GetValue<int?>("GameSessionSettings:ServerLeaseSeconds") ?? 90;

        if (serverLeaseSeconds is < 30 or > 600)
        {
            throw new InvalidOperationException("GameSessionSettings:ServerLeaseSeconds must be between 30 and 600 seconds.");
        }

        var allowDirectTransport = builder.Configuration.GetValue<bool?>("GameSessionSettings:AllowDirectTransport") ?? builder.Environment.IsDevelopment();
        var gameSessionService = new InMemoryGameSessionService(timeProvider, TimeSpan.FromSeconds(ticketLifetimeSeconds), TimeSpan.FromSeconds(serverLeaseSeconds), allowDirectTransport);

        builder.Services.AddSingleton<IGameSessionService>(gameSessionService);
        builder.Services.AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>();

        if (builder.Configuration.GetValue<bool>("UseInMemoryDatabase"))
        {
            builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
            {
                options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
                options.UseInMemoryDatabase("ProjectX");
            });
        }
        else
        {
            builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
            {
                options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"), b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName));
                options.ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
            });
        }

        builder.Services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
        builder.Services.AddScoped<ApplicationDbContextInitialiser>();

        builder.Services.AddAuthorizationBuilder();

        var jwtSettings = builder.Configuration.GetSection("JwtSettings");
        var securityKey = jwtSettings["SecurityKey"] ?? throw new InvalidOperationException("JwtSettings:SecurityKey is required.");
        var validIssuer = jwtSettings["ValidIssuer"] ?? throw new InvalidOperationException("JwtSettings:ValidIssuer is required.");
        var validAudience = jwtSettings["ValidAudience"] ?? throw new InvalidOperationException("JwtSettings:ValidAudience is required.");

        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = validIssuer,
            ValidAudience = validAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(securityKey)),
            LifetimeValidator = (notBefore, expires, securityToken, _) => JwtHandler.ValidateLifetime(notBefore, expires, securityToken, timeProvider.GetUtcNow().UtcDateTime),
            ClockSkew = TimeSpan.Zero
        };

        builder.Services.AddSingleton(tokenValidationParameters);

        builder.Services.AddAuthentication(opt =>
        {
            opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            opt.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;

        }).AddJwtBearer(options =>
        {
            options.TokenValidationParameters = tokenValidationParameters; // TODO: add from di?
        });

        builder.Services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                options.Lockout.MaxFailedAccessAttempts = 5;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders()
            .AddApiEndpoints();

        builder.Services.Configure<DataProtectionTokenProviderOptions>(opt => opt.TokenLifespan = TimeSpan.FromHours(2));

        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy(Policies.Server, policy => policy.RequireRole(Roles.Server));
            options.AddPolicy(Policies.Client, policy => policy.RequireRole(Roles.Client));
            options.AddPolicy(Policies.ServerOrClient, policy => policy.RequireRole(Roles.Server, Roles.Client));
            options.AddPolicy(Policies.ServerPlayerSession, policy =>
            {
                policy.RequireRole(Roles.Server);
                policy.AddRequirements(new PlayerSessionAuthorizationRequirement());
            });
        });

        builder.Services.AddScoped<JwtHandler>();
    }
}
