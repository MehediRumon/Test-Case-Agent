namespace TestCaseAgent.Server.Models;

public class AgentQuery
{
    public required string Question { get; set; }
    public string? Context { get; set; }
}

public class AgentResponse
{
    public required string Answer { get; set; }
    public List<string> References { get; set; } = new();
    public DateTime ResponseTime { get; set; } = DateTime.UtcNow;
}

public class TestCaseGenerationRequest
{
    public required string RequirementText { get; set; }
    public required string RequirementId { get; set; }
    public TestCasePriority Priority { get; set; } = TestCasePriority.Medium;
}

public class AuditLog
{
    public int Id { get; set; }
    public required string UserId { get; set; }
    public required string Action { get; set; }
    public required string Details { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? DocumentId { get; set; }
}