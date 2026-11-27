using TPai.Models;

namespace TPai.Repositories;

public interface IAuditLogRepository
{
    Task<IEnumerable<AuditLog>> GetAllAsync();
    Task LogAsync(string action, string entity, string details, string userEmail);
}
