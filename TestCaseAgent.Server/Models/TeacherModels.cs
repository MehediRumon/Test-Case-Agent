namespace TestCaseAgent.Server.Models;

public class Teacher
{
    public int Id { get; set; }
    public required string UserId { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
    public required string PinHash { get; set; }
    public DateTime PinCreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime PinExpiresAt { get; set; }
    public int FailedAttempts { get; set; } = 0;
    public DateTime? LockedUntil { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
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