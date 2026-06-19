using System.Net.Http.Headers;
using System.Net.Http.Json;
using Contracts.Admin.Dto;

namespace InShop.IntegrationTests.Api;

internal static class ApiTestSupport
{
    public const string TestAdminEmail = "admin@inshop.test";
    public const string TestAdminPassword = "TestAdmin1";

    public static void UseSession(this HttpClient client, Guid sessionToken)
    {
        client.DefaultRequestHeaders.Remove("X-Session-Token");
        client.DefaultRequestHeaders.Add("X-Session-Token", sessionToken.ToString());
    }

    public static void UseBearerToken(this HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public static async Task<string> RegisterAdminAndGetTokenAsync(HttpClient client)
    {
        var register = new AdminRegisterDto
        {
            Email = TestAdminEmail,
            Password = TestAdminPassword
        };

        var response = await client.PostAsJsonAsync("/api/Admin/auth/register", register);
        response.EnsureSuccessStatusCode();

        var auth = await response.Content.ReadFromJsonAsync<AdminAuthResponseDto>();
        return auth!.Token;
    }
}
