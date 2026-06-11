namespace ATLAS.IntegrationTests.Auth;

/// <summary>
/// Constants for the test authentication scheme used by integration tests.
/// Tests authenticate via TestAuthHandler, not Entra ID, to avoid external
/// dependencies and enable configurable role/claims scenarios.
/// </summary>
public static class TestAuthDefaults
{
    public const string AuthenticationScheme = "TestAuth";
}
