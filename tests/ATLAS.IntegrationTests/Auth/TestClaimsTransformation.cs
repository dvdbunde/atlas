using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace ATLAS.IntegrationTests.Auth;

public class TestClaimsTransformation : IClaimsTransformation
{
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        return Task.FromResult(principal);
    }
}
