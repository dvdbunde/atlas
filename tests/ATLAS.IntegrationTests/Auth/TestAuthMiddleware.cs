using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace ATLAS.IntegrationTests.Auth;

public class TestAuthMiddleware
{
    private readonly RequestDelegate _next;

    public TestAuthMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("X-Test-Identity", out var headerValue))
        {          
            try
            {
                var base64 = headerValue.ToString();
                var json = Encoding.UTF8.GetString(Convert.FromBase64String(base64));
                var dto = JsonSerializer.Deserialize<TestIdentityDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (dto != null)
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, dto.UserId ?? Guid.NewGuid().ToString()),
                        new Claim(ClaimTypes.Name, dto.Name ?? "Test User"),
                        new Claim(ClaimTypes.Email, dto.Email ?? "test@atlas.test"),
                    };

                    if (dto.Roles != null)
                    {
                        foreach (var role in dto.Roles)
                        {
                            claims.Add(new Claim(ClaimTypes.Role, role));
                        }
                    }

                    var identity = new ClaimsIdentity(claims, TestAuthDefaults.AuthenticationScheme);
                    var principal = new ClaimsPrincipal(identity);
                    context.Items[TestAuthHandler.UserIdentityKey] = principal;
                }
            }
            catch
            {
            }            
        }

        await _next(context);
    }

    private class TestIdentityDto
    {
        public string? UserId { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public List<string>? Roles { get; set; }
    }
}
