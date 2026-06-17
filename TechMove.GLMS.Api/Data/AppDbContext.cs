using Microsoft.EntityFrameworkCore;
using TechMove.GLMS.Core.Entities;

namespace TechMove.GLMS.Api.Data
{
    // Runtime API AppDbContext lives here; this file intentionally fresh to avoid
    // conflicts with the legacy MVC/Test DbContext definitions elsewhere.
    // TODO: move API migration/snapshot and shared wiring into this context if
    // EF tooling is used.
}
