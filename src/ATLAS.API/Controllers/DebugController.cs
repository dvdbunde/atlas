using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ATLAS.API.Controllers;

[ApiController]
[Route("api/debug")]
public class DebugController : ControllerBase
{
    /// <summary>
    /// Returns all claims from the authenticated user.
    /// Temporary endpoint for Entra ID troubleshooting.
    /// </summary>
    [Authorize]
    [HttpGet("claims")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult GetClaims()
    {
        var claims = User.Claims
            .Select(c => new
            {
                Type = c.Type,
                Value = c.Value
            })
            .OrderBy(c => c.Type);

        return Ok(claims);
    }

    /// <summary>
    /// Returns common identity information extracted from the JWT.
    /// Temporary endpoint for Entra ID troubleshooting.
    /// </summary>
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult GetCurrentUser()
    {
        return Ok(new
        {
            AuthenticationType = User.Identity?.AuthenticationType,
            IsAuthenticated = User.Identity?.IsAuthenticated,
            Name = User.Identity?.Name,

            NameIdentifier = User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
            NameClaim = User.FindFirst(ClaimTypes.Name)?.Value,

            Roles = User.FindAll(ClaimTypes.Role)
                .Select(r => r.Value)
                .ToArray(),

            AllClaims = User.Claims
                .Select(c => new
                {
                    c.Type,
                    c.Value
                })
                .OrderBy(c => c.Type)
                .ToArray()
        });
    }
}