using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using ATLAS.IntegrationTests.Auth;

namespace ATLAS.IntegrationTests;

public static class TestHttpContextExtensions
{
    private static HttpRequestMessage CreateRequest(
        HttpMethod method,
        string requestUri,
        object? body,
        ClaimsPrincipal? identity)
    {
        var message = new HttpRequestMessage(method, requestUri);

        if (body != null)
        {
            var json = JsonSerializer.Serialize(body, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            message.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        if (identity != null)
        {
            var dto = new
            {
                userId = identity.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                name = identity.FindFirst(ClaimTypes.Name)?.Value,
                email = identity.FindFirst(ClaimTypes.Email)?.Value,
                roles = identity.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList()
            };
            var serialized = JsonSerializer.Serialize(dto);
            message.Headers.Add("X-Test-Identity", Convert.ToBase64String(Encoding.UTF8.GetBytes(serialized)));
        }

        return message;
    }

    public static async Task<HttpResponseMessage> GetAsAsync(this HttpClient client, string requestUri, TestUserBuilder userBuilder)
    {
        var identity = userBuilder.Build();
        return await client.SendAsync(CreateRequest(HttpMethod.Get, requestUri, null, identity));
    }

    public static async Task<HttpResponseMessage> PostAsAsync(this HttpClient client, string requestUri, object body, TestUserBuilder userBuilder)
    {
        var identity = userBuilder.Build();
        return await client.SendAsync(CreateRequest(HttpMethod.Post, requestUri, body, identity));
    }

    public static async Task<HttpResponseMessage> PutAsAsync(this HttpClient client, string requestUri, object body, TestUserBuilder userBuilder)
    {
        var identity = userBuilder.Build();
        return await client.SendAsync(CreateRequest(HttpMethod.Put, requestUri, body, identity));
    }

    public static async Task<HttpResponseMessage> DeleteAsAsync(this HttpClient client, string requestUri, TestUserBuilder userBuilder)
    {
        var identity = userBuilder.Build();
        return await client.SendAsync(CreateRequest(HttpMethod.Delete, requestUri, null, identity));
    }

    public static async Task<HttpResponseMessage> GetAnonymousAsync(this HttpClient client, string requestUri)
    {
        var message = new HttpRequestMessage(HttpMethod.Get, requestUri);
        message.Headers.Add("X-Test-Identity", "ANONYMOUS");
        return await client.SendAsync(message);
    }

    public static async Task<HttpResponseMessage> PostAnonymousAsync(this HttpClient client, string requestUri, object body)
    {
        var json = JsonSerializer.Serialize(body, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        var message = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        message.Headers.Add("X-Test-Identity", "ANONYMOUS");
        return await client.SendAsync(message);
    }
}
