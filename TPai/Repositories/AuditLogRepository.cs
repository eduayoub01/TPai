using Microsoft.EntityFrameworkCore;
using TPai.Data;
using TPai.Models;

namespace TPai.Repositories;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly RentCarDbContext _context;

    public AuditLogRepository(RentCarDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<AuditLog>> GetAllAsync()
    {
        return await _context.AuditLogs
            .OrderByDescending(l => l.Timestamp)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task LogAsync(string action, string entity, string details, string userEmail)
    {
        _context.AuditLogs.Add(new AuditLog
        {
            Action = action,
            Entity = entity,
            Details = details,
            UserEmail = userEmail,
            Timestamp = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
    }
}
