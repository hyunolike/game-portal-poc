extern alias AdminApi;
extern alias WebApi;

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;

namespace GamePortal.IntegrationTests.Infrastructure;

public static class TestJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };
}

public static class HttpClientExtensions
{
    public static async Task<HttpClient> CreatePlayerClientAsync(this WebApplicationFactory<WebApi::Program> factory, long accountId)
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/dev/token", new { accountId });
        response.EnsureSuccessStatusCode();
        var token = await response.Content.ReadFromJsonAsync<TokenResponse>(TestJson.Options);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!.AccessToken);
        return client;
    }

    public static async Task<HttpClient> CreateOperatorClientAsync(this WebApplicationFactory<AdminApi::Program> factory, params string[] roles)
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/dev/token", new { operatorId = 9001, name = "tester", roles });
        response.EnsureSuccessStatusCode();
        var token = await response.Content.ReadFromJsonAsync<TokenResponse>(TestJson.Options);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!.AccessToken);
        return client;
    }

    public static async Task<T> ReadAsAsync<T>(this HttpResponseMessage response)
    {
        var value = await response.Content.ReadFromJsonAsync<T>(TestJson.Options);
        return value ?? throw new InvalidOperationException("Empty response body");
    }

    public static async Task<string?> ReadErrorCodeAsync(this HttpResponseMessage response)
    {
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.TryGetProperty("code", out var code) ? code.GetString() : null;
    }

    private sealed record TokenResponse(string AccessToken);
}
