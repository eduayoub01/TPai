using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TPai.Models;
using TPai.Repositories;

namespace TPai.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "ADMIN")]
public class LogsController : ControllerBase
{
    private readonly IAuditLogRepository _logRepository;

    public LogsController(IAuditLogRepository logRepository)
    {
        _logRepository = logRepository;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AuditLog>>> GetAll()
    {
        var logs = await _logRepository.GetAllAsync();
        return Ok(logs);
    }
}
