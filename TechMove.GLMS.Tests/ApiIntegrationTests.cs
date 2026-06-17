using System.Net.Http.Json;
using TechMove.GLMS.Core.DTOs.Contracts;
using TechMove.GLMS.Core.DTOs.ServiceRequests;
using Xunit;

namespace TechMove.GLMS.IntegrationTests;

public class ApiIntegrationTests : IClassFixture<InMemoryApiFixture>
{
    private readonly HttpClient _client;

    public ApiIntegrationTests(InMemoryApiFixture fixture)
    {
        _client = fixture.Client;
    }

    [Fact]
    public async Task GetClients_ReturnsSuccessAndNonNull()
    {
        var response = await _client.GetAsync("/api/clients");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

        var clients = await response.Content.ReadFromJsonAsync<List<object>>();
        Assert.NotNull(clients);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        var payload = new { email = "invalid@example.com", password = "wrong" };
        var response = await _client.PostAsJsonAsync("/api/auth/login", payload);

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
