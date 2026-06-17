namespace TechMove.GLMS.Core.Contracts;

public interface IContractService
{
    Task<IReadOnlyList<object>> GetAllAsync(Core.DTOs.Contracts.ContractListFilter filter, CancellationToken ct = default);
    Task<object?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<object> CreateAsync(Core.DTOs.Contracts.ContractCreateRequest request, CancellationToken ct = default);
    Task UpdateStatusAsync(int id, TechMove.GLMS.Core.Entities.ContractStatus status, CancellationToken ct = default);
}
