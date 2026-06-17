using Microsoft.AspNetCore.Http;

namespace TechMove.GLMS.Api.Services;

public interface IFileStorage
{
    Task<string> SaveAsync(IFormFile file, CancellationToken ct = default);
}
