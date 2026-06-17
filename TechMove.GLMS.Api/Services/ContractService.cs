using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechMove.GLMS.Core.Contracts;
using TechMove.GLMS.Core.DTOs.Contracts;
using TechMove.GLMS.Core.Repositories;
using TechMove.GLMS.Data;
using TechMove.GLMS.Models;
using TechMove.GLMS.Models.Enums;

namespace TechMove.GLMS.Api.Services;

public interface IContractService
{
    Task<IReadOnlyList<object>> GetAllAsync(ContractListFilter filter, CancellationToken ct = default);
    Task<object?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<object> CreateAsync(ContractCreateRequest request, CancellationToken ct = default);
    Task UpdateStatusAsync(int id, ContractStatus status, CancellationToken ct = default);
}

public class ContractService : IContractService
{
    private readonly AppDbContext _db;
    private readonly IFileStorage _fileStorage;
    private readonly ILogger<ContractService> _logger;

    private static readonly string[] AllowedExtensions = { ".pdf" };
    private const long MaxFileSize = 5 * 1024 * 1024;

    public ContractService(
        AppDbContext db,
        IFileStorage fileStorage,
        ILogger<ContractService> logger)
    {
        _db = db;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    private static (bool IsValid, string Message) ValidateFileUpload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return (false, "No file was uploaded.");

        if (file.Length > MaxFileSize)
            return (false, $"File size exceeds the {MaxFileSize / 1024 / 1024}MB limit.");

        var extension = Path.GetExtension(file.FileName).ToLower();
        if (!AllowedExtensions.Contains(extension))
            return (false, $"Only {string.Join(", ", AllowedExtensions).ToUpper()} files are allowed.");

        return (true, "Valid");
    }

    private static (bool IsValid, string Message) ValidateDateRange(DateTime startDate, DateTime endDate)
    {
        if (endDate <= startDate)
            return (false, "End date must be after start date.");

        return (true, "Valid");
    }

    public async Task<IReadOnlyList<object>> GetAllAsync(Core.DTOs.Contracts.ContractListFilter filter, CancellationToken ct = default)
    {
        var query = _db.Contracts
            .Include(c => c.Client)
            .AsQueryable();

        if (filter.ClientId.HasValue)
            query = query.Where(c => c.ClientId == filter.ClientId.Value);

        if (filter.StartFrom.HasValue)
            query = query.Where(c => c.StartDate >= filter.StartFrom.Value);

        if (filter.EndTo.HasValue)
            query = query.Where(c => c.EndDate <= filter.EndTo.Value);

        if (filter.Status.HasValue)
            query = query.Where(c => (int)c.Status == filter.Status.Value);

        var data = await query
            .OrderByDescending(c => c.StartDate)
            .Select(c => new
            {
                c.Id,
                c.ClientId,
                Client = new { c.Client.Id, c.Client.Name, c.Client.Region },
                c.StartDate,
                c.EndDate,
                c.Status,
                c.ServiceLevel,
                c.SignedAgreementPath,
                DaysRemaining = (int)Math.Max(0, (c.EndDate - DateTime.UtcNow).TotalDays)
            })
            .ToListAsync(ct);

        return data;
    }

    public async Task<object?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var contract = await _db.Contracts
            .Include(c => c.Client)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (contract == null)
            return null;

        return new
        {
            contract.Id,
            contract.ClientId,
            Client = new { contract.Client.Id, contract.Client.Name, contract.Client.Region },
            contract.StartDate,
            contract.EndDate,
            contract.Status,
            contract.ServiceLevel,
            contract.SignedAgreementPath,
            DaysRemaining = (int)Math.Max(0, (contract.EndDate - DateTime.UtcNow).TotalDays)
        };
    }

    public async Task<object> CreateAsync(ContractCreateRequest request, CancellationToken ct = default)
    {
        if (request.ClientId <= 0)
            throw new InvalidOperationException("Please select a valid client.");

        var dateValidation = ValidateDateRange(request.StartDate, request.EndDate);
        if (!dateValidation.IsValid)
            throw new InvalidOperationException(dateValidation.Message);

        if (!await _db.Clients.AnyAsync(c => c.Id == request.ClientId, ct))
            throw new InvalidOperationException("Selected client does not exist.");

        var fileValidation = ValidateFileUpload(request.SignedAgreement ?? new FormFile(new MemoryStream(), 0, 0, "signedAgreement", string.Empty));
        if (!fileValidation.IsValid && request.SignedAgreement is not null && request.SignedAgreement.Length > 0)
            throw new InvalidOperationException(fileValidation.Message);

        var contract = new TechMove.GLMS.Core.Entities.Contract
        {
            ClientId = request.ClientId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            ServiceLevel = request.ServiceLevel,
            Status = TechMove.GLMS.Core.Entities.ContractStatus.Draft
        };

        if (request.SignedAgreement is { Length: > 0 })
        {
            contract.SignedAgreementPath = await _fileStorage.SaveAsync(request.SignedAgreement, ct);
        }

        _db.Contracts.Add(contract);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Contract {Id} created via API service.", contract.Id);

        return new
        {
            contract.Id,
            contract.ClientId,
            contract.StartDate,
            contract.EndDate,
            contract.Status,
            contract.ServiceLevel,
            contract.SignedAgreementPath
        };
    }

    public async Task UpdateStatusAsync(int id, ContractStatus status, CancellationToken ct = default)
    {
        var contract = await _db.Contracts.FindAsync(new object?[] { id }, cancellationToken: ct);
        if (contract == null)
            throw new KeyNotFoundException("Contract not found.");

        if (!Enum.IsDefined(status))
            throw new InvalidOperationException("Invalid status value.");

        var entityStatus = TechMove.GLMS.Core.Entities.ContractStatus.Draft;
        if (status == (TechMove.GLMS.Models.Enums.ContractStatus)1) entityStatus = TechMove.GLMS.Core.Entities.ContractStatus.Active;
        if (status == (TechMove.GLMS.Models.Enums.ContractStatus)2) entityStatus = TechMove.GLMS.Core.Entities.ContractStatus.Expired;
        if (status == (TechMove.GLMS.Models.Enums.ContractStatus)3) entityStatus = TechMove.GLMS.Core.Entities.ContractStatus.OnHold;

        contract.Status = entityStatus;
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Contract {Id} status changed to {Status}.", id, status);
    }
}
