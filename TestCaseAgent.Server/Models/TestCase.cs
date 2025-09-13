namespace TestCaseAgent.Server.Models;

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
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public required string CreatedBy { get; set; }
}

public enum TestCasePriority
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}