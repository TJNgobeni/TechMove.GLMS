namespace TechMove.GLMS.Core.Contracts;

public interface IContractRepository
{
    Task<IReadOnlyList<Entities.Contract>> GetAllAsync(CancellationToken ct = default);
    Task<Entities.Contract?> GetByIdAsync(int id, CancellationToken ct = default);
    Task AddAsync(Entities.Contract contract, CancellationToken ct = default);
    Task UpdateAsync(Entities.Contract contract, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
