using System.ComponentModel.DataAnnotations;

namespace TPai.Models;

public class AuditLog
{
    public int Id { get; set; }

    [Required]
    [MaxLength(20)]
    public string Action { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Entity { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Details { get; set; } = string.Empty;

    [Required]
    [MaxLength(256)]
    public string UserEmail { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
