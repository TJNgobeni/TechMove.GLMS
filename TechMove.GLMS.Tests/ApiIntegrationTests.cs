using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
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
    public async Task Health_ApiRoot_ReturnsSuccess()
    {
        var response = await _client.GetAsync("/");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Contracts_ApiSurface_IsReachable()
    {
        var filter = new ContractListFilter();
        var query = new System.Collections.Generic.Dictionary<string, string?>
        {
            ["status"] = string.Empty,
            ["startDate"] = string.Empty,
            ["endDate"] = string.Empty
        };

        var response = await _client.GetAsync("/api/contracts?" + new FormUrlEncodedContent(query).ReadAsStringAsync());

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SwaggerDoc_IsAvailable()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }
}
