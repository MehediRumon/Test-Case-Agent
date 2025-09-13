namespace TestCaseAgent.Server.Models;

public class DocumentLink
{
    public int Id { get; set; }
    public required string UserId { get; set; }
    public required string DocumentId { get; set; }
    public required string DocumentTitle { get; set; }
    public required string DocumentUrl { get; set; }
    public DocumentType Type { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}

public enum DocumentType
{
    FunctionalRequirementSpec,
    TestCaseSheet
}