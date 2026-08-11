using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Application.GameSessions;
using ProjectX.Infrastructure.Identity;
using ProjectX.Infrastructure.Localization;
using ProjectX.Infrastructure.Persistance;
using ProjectX.Infrastructure.Persistance.Interceptors;

namespace ProjectX.Infrastructure;

public static class DependencyInjection
{
    public static void AddInfrastructureServices(this IHostApplicationBuilder builder)
    {
        var timeProvider = TimeProvider.System;
        builder.Services.AddSingleton<TimeProvider>(timeProvider);

        AddGameSessions(builder, timeProvider);
        AddPersistence(builder);
        AddIdentityAndAuthentication(builder, timeProvider);

        builder.Services.AddScoped<ITranslateService, JsonFileTranslateService>();
    }

    private static void AddGameSessions(IHostApplicationBuilder builder, TimeProvider timeProvider)
    {
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
        var gameSessionService = new GameSessionService(
            timeProvider,
            TimeSpan.FromSeconds(ticketLifetimeSeconds),
            TimeSpan.FromSeconds(serverLeaseSeconds),
            allowDirectTransport);

        builder.Services.AddSingleton<IGameSessionService>(gameSessionService);
    }

    private static void AddPersistence(IHostApplicationBuilder builder)
    {
        builder.Services.AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>();

        if (builder.Configuration.GetValue<bool>("UseInMemoryDatabase"))
        {
            builder.Services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
            {
                options.AddInterceptors(serviceProvider.GetServices<ISaveChangesInterceptor>());
                options.UseInMemoryDatabase("ProjectX");
            });
        }
        else
        {
            builder.Services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
            {
                options.AddInterceptors(serviceProvider.GetServices<ISaveChangesInterceptor>());
                options.UseSqlServer(
                    builder.Configuration.GetConnectionString("DefaultConnection"),
                    sql => sql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName));
                options.ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
            });
        }

        builder.Services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
        builder.Services.AddScoped<ApplicationDbContextInitialiser>();
    }

    private static void AddIdentityAndAuthentication(IHostApplicationBuilder builder, TimeProvider timeProvider)
    {
        var jwtSettings = builder.Configuration.GetSection("JwtSettings");
        var jwtOptions = new JwtOptions(
            jwtSettings["SecurityKey"] ?? throw new InvalidOperationException("JwtSettings:SecurityKey is required."),
            jwtSettings["ValidIssuer"] ?? throw new InvalidOperationException("JwtSettings:ValidIssuer is required."),
            jwtSettings["ValidAudience"] ?? throw new InvalidOperationException("JwtSettings:ValidAudience is required."));

        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.ValidIssuer,
            ValidAudience = jwtOptions.ValidAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecurityKey)),
            LifetimeValidator = (notBefore, expires, securityToken, _) => JwtAccessTokenService.ValidateLifetime(notBefore, expires, securityToken, timeProvider.GetUtcNow().UtcDateTime),
            ClockSkew = TimeSpan.Zero
        };

        builder.Services.AddSingleton(jwtOptions);
        builder.Services.AddSingleton(tokenValidationParameters);
        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(options => options.TokenValidationParameters = tokenValidationParameters);

        builder.Services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                options.Lockout.MaxFailedAccessAttempts = 5;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        builder.Services.Configure<DataProtectionTokenProviderOptions>(options => options.TokenLifespan = TimeSpan.FromHours(2));
        builder.Services.AddScoped<IApplicationUserAuthenticationService, ApplicationUserAuthenticationService>();
        builder.Services.AddScoped<IAccessTokenService, JwtAccessTokenService>();
    }
}
