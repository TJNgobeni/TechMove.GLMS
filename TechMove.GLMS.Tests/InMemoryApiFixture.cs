using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using TechMove.GLMS.Core.DTOs.Contracts;
using TechMove.GLMS.Core.DTOs.ServiceRequests;

namespace TechMove.GLMS.IntegrationTests;

public class InMemoryApiFixture
{
    public HttpClient Client { get; }

    public InMemoryApiFixture()
    {
        var factory = new WebApplicationFactory<TechMove.GLMS.Api.Controllers.ClientsController>();

        Client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = true });
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
