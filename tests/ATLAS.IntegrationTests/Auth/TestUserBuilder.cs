using System.Security.Claims;

namespace ATLAS.IntegrationTests.Auth;

/// <summary>
/// Builder for creating test user identities with configurable roles,
/// claims, and user IDs. Used by TestAuthHandler to construct the
/// ClaimsPrincipal for integration test requests.
/// </summary>
public class TestUserBuilder
{
    private Guid _userId = Guid.NewGuid();
    private string _name = "Test User";
    private string _email = "test@atlas.test";
    private readonly List<Claim> _additionalClaims = new();

    private TestUserBuilder() { }

    public static TestUserBuilder AsCitizen() => new()
    {
        _userId = TestData.CitizenUserId != Guid.Empty ? TestData.CitizenUserId : Guid.NewGuid(),
        _name = "Test Citizen",
        _email = "citizen@atlas.test",
        _additionalClaims = { new Claim(ClaimTypes.Role, "Citizen") }
    };

    public static TestUserBuilder AsOfficer() => new()
    {
        _userId = TestData.OfficerUserId != Guid.Empty ? TestData.OfficerUserId : Guid.NewGuid(),
        _name = "Test Officer",
        _email = "officer@atlas.test",
        _additionalClaims = { new Claim(ClaimTypes.Role, "Officer") }
    };

    public static TestUserBuilder AsAdmin() => new()
    {
        _userId = TestData.AdminUserId != Guid.Empty ? TestData.AdminUserId : Guid.NewGuid(),
        _name = "Test Admin",
        _email = "admin@atlas.test",
        _additionalClaims = { new Claim(ClaimTypes.Role, "Admin") }
    };

    public static TestUserBuilder AsUser(Guid userId, string name, string email, string role) => new()
    {
        _userId = userId,
        _name = name,
        _email = email,
        _additionalClaims = { new Claim(ClaimTypes.Role, role) }
    };

    public TestUserBuilder WithClaim(string type, string value)
    {
        _additionalClaims.Add(new Claim(type, value));
        return this;
    }

    public ClaimsPrincipal Build()
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, _userId.ToString()),
            new Claim(ClaimTypes.Name, _name),
            new Claim(ClaimTypes.Email, _email),
        };
        claims.AddRange(_additionalClaims);

        var identity = new ClaimsIdentity(claims, TestAuthDefaults.AuthenticationScheme);
        return new ClaimsPrincipal(identity);
    }
}

