using TestCaseAgent.Server.Models;

namespace TestCaseAgent.Server.Services;

public interface IAuditService
{
    Task LogActionAsync(string userId, string action, string details, string? documentId = null);
    Task<List<AuditLog>> GetUserAuditLogsAsync(string userId, int pageSize = 50, int page = 1);
    Task<List<AuditLog>> GetAllAuditLogsAsync(int pageSize = 50, int page = 1);
}

public class AuditService : IAuditService
{
    private readonly ILogger<AuditService> _logger;
    private readonly List<AuditLog> _auditLogs; // In-memory storage for demo

    public AuditService(ILogger<AuditService> logger)
    {
        _logger = logger;
        _auditLogs = new List<AuditLog>();
    }

    public async Task LogActionAsync(string userId, string action, string details, string? documentId = null)
    {
        try
        {
            var auditLog = new AuditLog
            {
                Id = _auditLogs.Count + 1,
                UserId = userId,
                Action = action,
                Details = details,
                DocumentId = documentId
            };

            _auditLogs.Add(auditLog);

            _logger.LogInformation("Audit log created: {Action} by user {UserId}", action, userId);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating audit log for user {UserId}", userId);
            throw;
        }
    }

    public async Task<List<AuditLog>> GetUserAuditLogsAsync(string userId, int pageSize = 50, int page = 1)
    {
        try
        {
            var logs = _auditLogs
                .Where(log => log.UserId == userId)
                .OrderByDescending(log => log.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return await Task.FromResult(logs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit logs for user {UserId}", userId);
            throw;
        }
    }

    public async Task<List<AuditLog>> GetAllAuditLogsAsync(int pageSize = 50, int page = 1)
    {
        try
        {
            var logs = _auditLogs
                .OrderByDescending(log => log.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return await Task.FromResult(logs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all audit logs");
            throw;
        }
    }
}