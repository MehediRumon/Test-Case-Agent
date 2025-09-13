namespace TestCaseAgent.Client.Models;

public class DocumentLink
{
    public int Id { get; set; }
    public required string UserId { get; set; }
    public required string DocumentId { get; set; }
    public required string DocumentTitle { get; set; }
    public required string DocumentUrl { get; set; }
    public DocumentType Type { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
}

public enum DocumentType
{
    FunctionalRequirementSpec,
    TestCaseSheet
}

public class TestCase
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public required string Preconditions { get; set; }
    public required string TestSteps { get; set; }
    public required string ExpectedResults { get; set; }
    public TestCasePriority Priority { get; set; }
    public required string RequirementReference { get; set; }
    public DateTime CreatedAt { get; set; }
    public required string CreatedBy { get; set; }
}

public enum TestCasePriority
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

public class AgentQuery
{
    public required string Question { get; set; }
    public string? Context { get; set; }
}

public class AgentResponse
{
    public required string Answer { get; set; }
    public List<string> References { get; set; } = new();
    public DateTime ResponseTime { get; set; }
}

public class TestCaseGenerationRequest
{
    public required string RequirementText { get; set; }
    public required string RequirementId { get; set; }
    public TestCasePriority Priority { get; set; } = TestCasePriority.Medium;
}

public class CreateTestCaseRequest
{
    public required string Prompt { get; set; }
}

public class LinkDocumentRequest
{
    public required string DocumentId { get; set; }
    public string DocumentTitle { get; set; } = "";
    public required string DocumentUrl { get; set; }
    public DocumentType Type { get; set; }
}

public class AgentStatus
{
    public bool HasFRSDocument { get; set; }
    public bool HasTestCaseSheet { get; set; }
    public string? FRSDocumentTitle { get; set; }
    public string? TestCaseSheetTitle { get; set; }
    public bool IsReady { get; set; }
}

public class AuditLog
{
    public int Id { get; set; }
    public required string UserId { get; set; }
    public required string Action { get; set; }
    public required string Details { get; set; }
    public DateTime Timestamp { get; set; }
    public string? DocumentId { get; set; }
}

// Teacher PIN related models
public class Teacher
{
    public int Id { get; set; }
    public required string UserId { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime PinExpiresAt { get; set; }
    public int FailedAttempts { get; set; }
    public bool IsLocked { get; set; }
    public DateTime? LockedUntil { get; set; }
}

public class TeacherPinRequest
{
    public required string Pin { get; set; }
}

public class TeacherPinResponse
{
    public bool IsValid { get; set; }
    public string Message { get; set; } = "";
    public bool IsLocked { get; set; }
    public DateTime? LockedUntil { get; set; }
    public int RemainingAttempts { get; set; }
    public bool RequiresReset { get; set; }
}

public class TeacherRegistrationRequest
{
    public required string Name { get; set; }
    public required string Email { get; set; }
    public required string Pin { get; set; }
}

public class TeacherPinValidationResult
{
    public bool IsValid { get; set; }
    public string Message { get; set; } = "";
    public List<string> ValidationErrors { get; set; } = new();
}