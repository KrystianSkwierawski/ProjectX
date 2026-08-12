using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using ProjectX.Application.ApplicationUsers.Commands.RefreshSession;
using ProjectX.Application.Common.Security;
using ProjectX.Domain.Enums;
using ProjectX.Infrastructure.Identity;
using JsonWebTokenHandler = Microsoft.IdentityModel.JsonWebTokens.JsonWebTokenHandler;

namespace ProjectX.Web.AcceptanceTests.Authentication;

public sealed class JwtAuthenticationPipelineTests : IClassFixture<JwtApiFactory>, IDisposable
{
    private readonly JwtApiFactory _factory;
    private readonly HttpClient _client;

    public JwtAuthenticationPipelineTests(JwtApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });
    }

    [Fact]
    public async Task EndpointWithoutExplicitPolicy_UsesFallbackAuthorization()
    {
        var response = await _client.GetAsync("/");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task LoginEndpoint_RemainsAnonymous()
    {
        var response = await _client.PostAsJsonAsync("/api/ApplicationUsers", new { UserName = string.Empty, Password = string.Empty });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task TokenWithInvalidSignature_IsRejected()
    {
        var token = CreateToken(securityKey: JwtApiFactory.AlternativeSecurityKey);

        var response = await SendAuthorizedAsync(HttpMethod.Get, "/api/Quests", token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ValidToken_SatisfiesConfiguredValidationParameters()
    {
        var parameters = _factory.Services.GetRequiredService<TokenValidationParameters>();
        var result = await new JsonWebTokenHandler().ValidateTokenAsync(CreateToken(), parameters);

        Assert.True(result.IsValid, result.Exception?.ToString());
    }

    [Theory]
    [InlineData("invalid-issuer", JwtApiFactory.Audience)]
    [InlineData(JwtApiFactory.Issuer, "invalid-audience")]
    public async Task TokenWithInvalidIssuerOrAudience_IsRejected(string issuer, string audience)
    {
        var token = CreateToken(issuer: issuer, audience: audience);

        var response = await SendAuthorizedAsync(HttpMethod.Get, "/api/Quests", token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ExpiredToken_IsRejected()
    {
        var now = DateTimeOffset.UtcNow;
        var token = CreateToken(
            notBeforeUtc: now.AddHours(-2),
            expiresAtUtc: now.AddHours(-1),
            sessionStartedAtUtc: now.AddHours(-2));

        var response = await SendAuthorizedAsync(HttpMethod.Get, "/api/Quests", token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task TokenExtendingPastMaximumSessionLifetime_IsRejected()
    {
        var now = DateTimeOffset.UtcNow;
        var token = CreateToken(
            notBeforeUtc: now.AddMinutes(-1),
            expiresAtUtc: now.AddMinutes(4),
            sessionStartedAtUtc: now.Subtract(SessionTokenPolicy.MaximumSessionLifetime).AddMinutes(-1));

        var response = await SendAuthorizedAsync(HttpMethod.Get, "/api/Quests", token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task TokenSignedWithUnsupportedAlgorithm_IsRejected()
    {
        var token = CreateToken(algorithm: SecurityAlgorithms.HmacSha384);

        var response = await SendAuthorizedAsync(HttpMethod.Get, "/api/Quests", token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task TokenWithoutRequiredSessionClaim_IsRejected(bool includeSessionStart, bool includeVersion)
    {
        var token = CreateToken(
            includeSessionStartedAtClaim: includeSessionStart,
            includeVersionClaim: includeVersion);

        var response = await SendAuthorizedAsync(HttpMethod.Get, "/api/Quests", token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ClientRole_CannotAccessServerEndpoint()
    {
        var token = CreateToken(role: ApplicationRoles.Client);

        var response = await SendAuthorizedAsync(HttpMethod.Post, "/api/GameSessions/Register", token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ServerWithoutPlayerSessionId_CannotAccessDelegatedPlayerEndpoint()
    {
        var token = CreateToken(role: ApplicationRoles.Server);

        var response = await SendAuthorizedAsync(HttpMethod.Get, "/api/Characters/1", token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RefreshBeforeFinalFiveMinutes_IsRejected()
    {
        await _factory.EnsureClientUserExistsAsync();
        var token = CreateToken();

        var response = await SendAuthorizedAsync(HttpMethod.Post, "/api/ApplicationUsers/RefreshSession", token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RefreshWithoutUserIdentifier_IsRejected()
    {
        var now = DateTimeOffset.UtcNow;
        var token = CreateToken(
            notBeforeUtc: now.AddMinutes(-56),
            expiresAtUtc: now.AddMinutes(4),
            sessionStartedAtUtc: now.AddMinutes(-56),
            includeUserIdClaim: false);

        var response = await SendAuthorizedAsync(HttpMethod.Post, "/api/ApplicationUsers/RefreshSession", token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task NearExpiryToken_CanBeRenewedWithoutResettingSessionStart()
    {
        await _factory.EnsureClientUserExistsAsync();
        var now = DateTimeOffset.UtcNow;
        var sessionStartedAt = now.AddMinutes(-56);
        var token = CreateToken(
            notBeforeUtc: sessionStartedAt,
            expiresAtUtc: now.AddMinutes(4),
            sessionStartedAtUtc: sessionStartedAt);

        var response = await SendAuthorizedAsync(HttpMethod.Post, "/api/ApplicationUsers/RefreshSession", token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<RefreshSessionDto>();
        var renewedToken = new JwtSecurityTokenHandler().ReadJwtToken(result!.Token);

        Assert.Equal(
            sessionStartedAt.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture),
            renewedToken.Claims.Single(claim => claim.Type == SessionTokenPolicy.SessionStartedAtClaim).Value);
        Assert.Contains(renewedToken.Claims, claim => claim.Type == ClaimTypes.Role && claim.Value == ApplicationRoles.Client);
        Assert.True(renewedToken.ValidTo > now.AddMinutes(59));
        Assert.True(renewedToken.ValidTo <= now.Add(SessionTokenPolicy.Lifetime));
    }

    [Fact]
    public async Task RenewalCannotExtendSessionPast24Hours()
    {
        await _factory.EnsureClientUserExistsAsync();
        var now = DateTimeOffset.UtcNow;
        var sessionStartedAt = now.Subtract(SessionTokenPolicy.MaximumSessionLifetime).AddMinutes(4);
        var sessionExpiresAt = sessionStartedAt.Add(SessionTokenPolicy.MaximumSessionLifetime);
        var token = CreateToken(
            notBeforeUtc: now.AddMinutes(-56),
            expiresAtUtc: sessionExpiresAt,
            sessionStartedAtUtc: sessionStartedAt);

        var response = await SendAuthorizedAsync(HttpMethod.Post, "/api/ApplicationUsers/RefreshSession", token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<RefreshSessionDto>();
        var renewedToken = new JwtSecurityTokenHandler().ReadJwtToken(result!.Token);

        Assert.Equal(sessionExpiresAt.ToUnixTimeSeconds(), new DateTimeOffset(renewedToken.ValidTo).ToUnixTimeSeconds());
    }

    private async Task<HttpResponseMessage> SendAuthorizedAsync(HttpMethod method, string path, string token)
    {
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await _client.SendAsync(request);
    }

    public void Dispose()
    {
        _client.Dispose();
    }

    private static string CreateToken(
        string securityKey = JwtApiFactory.SecurityKey,
        string issuer = JwtApiFactory.Issuer,
        string audience = JwtApiFactory.Audience,
        string role = ApplicationRoles.Client,
        string algorithm = SecurityAlgorithms.HmacSha256,
        DateTimeOffset? notBeforeUtc = null,
        DateTimeOffset? expiresAtUtc = null,
        DateTimeOffset? sessionStartedAtUtc = null,
        bool includeUserIdClaim = true,
        bool includeSessionStartedAtClaim = true,
        bool includeVersionClaim = true)
    {
        var now = DateTimeOffset.UtcNow;
        var notBefore = notBeforeUtc ?? now.AddMinutes(-1);
        var expiresAt = expiresAtUtc ?? now.AddMinutes(59);
        var sessionStartedAt = sessionStartedAtUtc ?? notBefore;
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Email, JwtApiFactory.Email),
            new Claim(ClaimTypes.Name, JwtApiFactory.Email),
            new Claim(ClaimTypes.Role, role),
            new Claim(nameof(LanguageEnum), LanguageEnum.en.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(
                JwtRegisteredClaimNames.Iat,
                now.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture),
                ClaimValueTypes.Integer64)
        };

        if (includeUserIdClaim)
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, JwtApiFactory.UserId));
        }

        if (includeSessionStartedAtClaim)
        {
            claims.Add(new Claim(
                SessionTokenPolicy.SessionStartedAtClaim,
                sessionStartedAt.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture),
                ClaimValueTypes.Integer64));
        }

        if (includeVersionClaim)
        {
            claims.Add(new Claim(SessionTokenPolicy.VersionClaim, SessionTokenPolicy.CurrentVersion));
        }

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(securityKey)),
            algorithm);
        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            notBefore.UtcDateTime,
            expiresAt.UtcDateTime,
            credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public sealed class JwtApiFactory : WebApplicationFactory<Program>
{
    public const string Audience = "ProjectX.Web.AcceptanceTests";
    public const string Email = "jwt-pipeline-user@example.com";
    public const string Issuer = "ProjectX.Web.AcceptanceTests";
    public const string SecurityKey = "projectx-web-acceptance-tests-security-key-with-more-than-64-bytes";
    public const string AlternativeSecurityKey = "projectx-web-acceptance-tests-alternative-key-with-more-than-64-bytes";
    public const string UserId = "jwt-pipeline-user";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("JwtSettings:SecurityKey", SecurityKey);
        builder.UseSetting("JwtSettings:ValidIssuer", Issuer);
        builder.UseSetting("JwtSettings:ValidAudience", Audience);
        builder.UseSetting("SkipDatabaseInitialization", bool.TrueString);
        builder.UseSetting("UseInMemoryDatabase", bool.TrueString);
        builder.ConfigureTestServices(services =>
        {
            var jwtOptions = new JwtOptions(SecurityKey, Issuer, Audience);
            var validationParameters = JwtAccessTokenService.CreateValidationParameters(jwtOptions, TimeProvider.System);

            services.RemoveAll<JwtOptions>();
            services.RemoveAll<TokenValidationParameters>();
            services.RemoveAll<IDataProtectionProvider>();
            services.AddSingleton(jwtOptions);
            services.AddSingleton(validationParameters);
            services.AddSingleton<IDataProtectionProvider>(new EphemeralDataProtectionProvider());
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
                options.TokenValidationParameters = validationParameters);
        });
    }

    public async Task EnsureClientUserExistsAsync()
    {
        using var scope = Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        if (!await roleManager.RoleExistsAsync(ApplicationRoles.Client))
        {
            EnsureSucceeded(await roleManager.CreateAsync(new IdentityRole(ApplicationRoles.Client)));
        }

        var user = await userManager.FindByIdAsync(UserId);

        if (user is null)
        {
            user = new ApplicationUser
            {
                Id = UserId,
                Email = Email,
                UserName = Email,
                Language = LanguageEnum.en
            };

            EnsureSucceeded(await userManager.CreateAsync(user));
        }

        if (!await userManager.IsInRoleAsync(user, ApplicationRoles.Client))
        {
            EnsureSucceeded(await userManager.AddToRoleAsync(user, ApplicationRoles.Client));
        }
    }

    private static void EnsureSucceeded(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(error => error.Description)));
        }
    }
}
