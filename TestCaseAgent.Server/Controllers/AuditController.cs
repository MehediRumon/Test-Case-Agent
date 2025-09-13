using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TestCaseAgent.Server.Models;
using TestCaseAgent.Server.Services;

namespace TestCaseAgent.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AuditController : ControllerBase
{
    private readonly IAuditService _auditService;
    private readonly ILogger<AuditController> _logger;

    public AuditController(IAuditService auditService, ILogger<AuditController> logger)
    {
        _auditService = auditService;
        _logger = logger;
    }

    [HttpGet("my-logs")]
    public async Task<ActionResult<List<AuditLog>>> GetMyAuditLogs([FromQuery] int pageSize = 50, [FromQuery] int page = 1)
    {
        try
        {
            var userId = GetUserId();
            var logs = await _auditService.GetUserAuditLogsAsync(userId, pageSize, page);
            return Ok(logs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit logs for user");
            return BadRequest($"Failed to retrieve audit logs: {ex.Message}");
        }
    }

    [HttpGet("all-logs")]
    [Authorize(Roles = "Admin")] // Only admins can view all logs
    public async Task<ActionResult<List<AuditLog>>> GetAllAuditLogs([FromQuery] int pageSize = 50, [FromQuery] int page = 1)
    {
        try
        {
            var logs = await _auditService.GetAllAuditLogsAsync(pageSize, page);
            return Ok(logs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all audit logs");
            return BadRequest($"Failed to retrieve audit logs: {ex.Message}");
        }
    }

    private string GetUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
    }
}