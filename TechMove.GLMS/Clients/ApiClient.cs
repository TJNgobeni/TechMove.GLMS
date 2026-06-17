using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using TechMove.GLMS.Models;
using TechMove.GLMS.Models.Enums;
using TechMove.GLMS.Core.DTOs.Contracts;
using TechMove.GLMS.Core.DTOs.ServiceRequests;

namespace TechMove.GLMS.Clients;

public interface IApiClient
{
    Task<IReadOnlyList<Models.Contract>> GetContractsAsync(ContractListFilter? filter = null, CancellationToken ct = default);
    Task<Models.Contract?> GetContractAsync(int id, CancellationToken ct = default);
    Task<Models.Contract> CreateContractAsync(ContractCreateRequest request, CancellationToken ct = default);
    Task PatchContractStatusAsync(int id, ContractStatus status, CancellationToken ct = default);

    Task<IReadOnlyList<Client>> GetClientsAsync(CancellationToken ct = default);

    Task<IReadOnlyList<Models.ServiceRequest>> GetServiceRequestsAsync(int? contractId = null, CancellationToken ct = default);
    Task<Models.ServiceRequest?> GetServiceRequestAsync(int id, CancellationToken ct = default);
    Task<Models.ServiceRequest> CreateServiceRequestAsync(ServiceRequestCreateRequest request, CancellationToken ct = default);
}

public class ApiClient : IApiClient, IAsyncDisposable
{
    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ApiClient(HttpClient http, IHttpContextAccessor httpContextAccessor)
    {
        _http = http;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IReadOnlyList<Models.Contract>> GetContractsAsync(ContractListFilter? filter = null, CancellationToken ct = default)
    {
        var query = filter != null ? Query(filter) : string.Empty;
        await ApplyAuthAsync(ct);
        using var response = await _http.GetAsync($"api/contracts{query}", ct);
        response.EnsureSuccessStatusCode();

        var items = await response.Content.ReadFromJsonAsync<List<ContractResponse>>(camelCaseJson, ct);
        return items?.Select(MapContract).ToList() ?? new List<Models.Contract>();
    }

    public async Task<Models.Contract?> GetContractAsync(int id, CancellationToken ct = default)
    {
        await ApplyAuthAsync(ct);
        using var response = await _http.GetAsync($"api/contracts/{id}", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        var item = await response.Content.ReadFromJsonAsync<ContractResponse>(camelCaseJson, ct);
        return item == null ? null : MapContract(item);
    }

    public async Task<Models.Contract> CreateContractAsync(ContractCreateRequest request, CancellationToken ct = default)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(request.ClientId.ToString()), nameof(request.ClientId));
        content.Add(new StringContent(request.StartDate.ToString("o")), nameof(request.StartDate));
        content.Add(new StringContent(request.EndDate.ToString("o")), nameof(request.EndDate));
        content.Add(new StringContent(request.ServiceLevel ?? string.Empty), nameof(request.ServiceLevel));

        if (request.SignedAgreement is { Length: > 0 })
        {
            var fileContent = new StreamContent(request.SignedAgreement.OpenReadStream());
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            content.Add(fileContent, nameof(request.SignedAgreement), request.SignedAgreement.FileName);
        }

        await ApplyAuthAsync(ct);
        using var response = await _http.PostAsync("api/contracts", content, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(body);

        var created = await response.Content.ReadFromJsonAsync<ContractResponse>(camelCaseJson, ct);
        if (created == null)
            throw new InvalidOperationException("Invalid response.");

        return MapContract(created);
    }

    public async Task PatchContractStatusAsync(int id, ContractStatus status, CancellationToken ct = default)
    {
        var payload = JsonSerializer.Serialize(new { status = (int)status }, camelCaseJson);
        using var content = new StringContent(payload, Encoding.UTF8, "application/json");
        await ApplyAuthAsync(ct);
        using var response = await _http.PatchAsync($"api/contracts/{id}/status", content, ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            throw new KeyNotFoundException("Contract not found.");
        response.EnsureSuccessStatusCode();
    }

    public async Task<IReadOnlyList<Client>> GetClientsAsync(CancellationToken ct = default)
    {
        await ApplyAuthAsync(ct);
        using var response = await _http.GetAsync("api/clients", ct);
        response.EnsureSuccessStatusCode();
        var items = await response.Content.ReadFromJsonAsync<List<ClientResponse>>(camelCaseJson, ct);
        return items?.Select(MapClient).ToList() ?? new List<Client>();
    }

    public async Task<IReadOnlyList<Models.ServiceRequest>> GetServiceRequestsAsync(int? contractId = null, CancellationToken ct = default)
    {
        var q = contractId.HasValue ? $"?contractId={contractId}" : string.Empty;
        await ApplyAuthAsync(ct);
        using var response = await _http.GetAsync($"api/servicerequests{q}", ct);
        response.EnsureSuccessStatusCode();

        var items = await response.Content.ReadFromJsonAsync<List<ServiceRequestResponse>>(camelCaseJson, ct);
        return items?.Select(MapServiceRequest).ToList() ?? new List<Models.ServiceRequest>();
    }

    public async Task<Models.ServiceRequest?> GetServiceRequestAsync(int id, CancellationToken ct = default)
    {
        await ApplyAuthAsync(ct);
        using var response = await _http.GetAsync($"api/servicerequests/{id}", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        var item = await response.Content.ReadFromJsonAsync<ServiceRequestResponse>(camelCaseJson, ct);
        return item == null ? null : MapServiceRequest(item);
    }

    public async Task<Models.ServiceRequest> CreateServiceRequestAsync(ServiceRequestCreateRequest request, CancellationToken ct = default)
    {
        var payload = JsonSerializer.Serialize(request, camelCaseJson);
        using var content = new StringContent(payload, Encoding.UTF8, "application/json");
        await ApplyAuthAsync(ct);
        using var response = await _http.PostAsync("api/servicerequests", content, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(body);

        var created = await response.Content.ReadFromJsonAsync<ServiceRequestResponse>(camelCaseJson, ct);
        if (created == null)
            throw new InvalidOperationException("Invalid response.");

        return MapServiceRequest(created);
    }

    public ValueTask DisposeAsync()
    {
        _http.Dispose();
        return ValueTask.CompletedTask;
    }

    private static Models.Contract MapContract(ContractResponse r) => new()
    {
        Id = r.Id,
        ClientId = r.ClientId,
        Client = r.Client != null ? new Client { Id = r.Client.Id, Name = r.Client.Name, Region = r.Client.Region } : null!,
        StartDate = r.StartDate,
        EndDate = r.EndDate,
        Status = r.Status,
        ServiceLevel = r.ServiceLevel ?? string.Empty,
        SignedAgreementPath = r.SignedAgreementPath ?? string.Empty
    };

    private static Client MapClient(ClientResponse r) => new()
    {
        Id = r.Id,
        Name = r.Name,
        Region = r.Region ?? string.Empty,
        ContactDetails = string.Empty
    };

    private static Models.ServiceRequest MapServiceRequest(ServiceRequestResponse r) => new()
    {
        Id = r.Id,
        ContractId = r.ContractId,
        Contract = null!,
        Description = r.Description ?? string.Empty,
        Cost = r.Cost,
        CostZAR = r.CostZAR,
        Status = r.Status ?? "Pending",
        CreatedAt = r.CreatedAt
    };

    private static string Query(ContractListFilter f)
    {
        var q = new List<string>();
        if (f.ClientId.HasValue) q.Add($"clientId={f.ClientId}");
        if (f.StartFrom.HasValue) q.Add($"startFrom={f.StartFrom.Value:o}");
        if (f.EndTo.HasValue) q.Add($"endTo={f.EndTo.Value:o}");
        if (f.Status.HasValue) q.Add($"status={(int)f.Status.Value}");
        return q.Count == 0 ? string.Empty : "?" + string.Join("&", q);
    }

    private Task ApplyAuthAsync(CancellationToken ct)
    {
        var token = _httpContextAccessor.HttpContext?.Session.GetString("AccessToken");
        _http.DefaultRequestHeaders.Authorization = string.IsNullOrEmpty(token)
            ? null
            : new AuthenticationHeaderValue("Bearer", token);

        return Task.CompletedTask;
    }

    private static readonly JsonSerializerOptions camelCaseJson = new(JsonSerializerDefaults.Web);
}

public class ContractResponse
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public ClientResponse? Client { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public ContractStatus Status { get; set; }
    public string? ServiceLevel { get; set; }
    public string? SignedAgreementPath { get; set; }
}

public class ClientResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Region { get; set; }
}

public class ServiceRequestResponse
{
    public int Id { get; set; }
    public int ContractId { get; set; }
    public string? Description { get; set; }
    public decimal Cost { get; set; }
    public decimal CostZAR { get; set; }
    public string? Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
