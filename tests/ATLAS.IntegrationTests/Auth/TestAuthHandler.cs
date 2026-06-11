using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ATLAS.IntegrationTests.Auth;

public class TestAuthHandler : AuthenticationHandler<TestAuthenticationOptions>
{
    public const string UserIdentityKey = "TestUserIdentity";

    public TestAuthHandler(
        IOptionsMonitor<TestAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {      
        if (Context.Items.TryGetValue(UserIdentityKey, out var identityObj) &&
            identityObj is ClaimsPrincipal principal)
        {
            var ticket = new AuthenticationTicket(principal, TestAuthDefaults.AuthenticationScheme);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier,
                TestData.AdminUserId != Guid.Empty
                    ? TestData.AdminUserId.ToString()
                    : Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Name, "Test Admin"),
            new Claim(ClaimTypes.Email, "admin@atlas.test"),
            new Claim(ClaimTypes.Role, "Admin")
        };
        var fallbackIdentity = new ClaimsIdentity(claims, TestAuthDefaults.AuthenticationScheme);
        var fallbackPrincipal = new ClaimsPrincipal(fallbackIdentity);
        var fallbackTicket = new AuthenticationTicket(fallbackPrincipal, TestAuthDefaults.AuthenticationScheme);

        return Task.FromResult(AuthenticateResult.Success(fallbackTicket));
    }
}

