using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ProjectX.Web.AcceptanceTests.Authentication;

public sealed class LoginRateLimitingTests
{
    [Fact]
    public async Task Login_AllowsFiveAttemptsThenRejectsNextAttemptForSameIp()
    {
        using var factory = new JwtApiFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });
        var request = new
        {
            UserName = "missing-user@example.com",
            Password = "IncorrectPassword1!"
        };

        for (var attempt = 0; attempt < 5; attempt++)
        {
            using var response = await client.PostAsJsonAsync("/api/ApplicationUsers", request);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        using var rejectedResponse = await client.PostAsJsonAsync("/api/ApplicationUsers", request);

        Assert.Equal(HttpStatusCode.TooManyRequests, rejectedResponse.StatusCode);
    }
}
