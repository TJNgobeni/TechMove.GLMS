using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using TechMove.GLMS.Core.DTOs.Contracts;
using TechMove.GLMS.Core.DTOs.ServiceRequests;

namespace TechMove.GLMS.IntegrationTests;

public class InMemoryApiFixture : IDisposable
{
    public HttpClient Client { get; }
    public WebApplicationFactory<Program> Factory { get; }

    public InMemoryApiFixture()
    {
        Factory = new WebApplicationFactory<Program>();
        Client = Factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = true });
    }

    public void Dispose()
    {
        Client.Dispose();
        Factory.Dispose();
    }
}

public class ContractResponse
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public string? ClientName { get; set; }
    public string? ServiceLevel { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? SignedAgreementPath { get; set; }
}
