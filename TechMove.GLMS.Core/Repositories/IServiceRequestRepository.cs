namespace TechMove.GLMS.Core.Contracts;

public interface IServiceRequestRepository
{
    Task<IReadOnlyList<Entities.ServiceRequest>> GetActiveByContractAsync(int contractId, CancellationToken ct = default);
    Task AddAsync(Entities.ServiceRequest serviceRequest, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
