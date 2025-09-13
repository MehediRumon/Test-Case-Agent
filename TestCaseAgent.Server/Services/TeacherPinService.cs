using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using TestCaseAgent.Server.Models;

namespace TestCaseAgent.Server.Services;

public interface ITeacherPinService
{
    Task<Teacher> RegisterTeacherAsync(string name, string email, string pin);
    Task<TeacherPinResponse> ValidatePinAsync(string userId, string pin);
    Task<TeacherPinValidationResult> ValidatePinFormatAsync(string pin);
    Task<Teacher?> GetTeacherAsync(string userId);
    Task<bool> ResetPinAsync(string userId, string newPin);
    Task<bool> UnlockAccountAsync(string userId);
}

public class TeacherPinService : ITeacherPinService
{
    private readonly ILogger<TeacherPinService> _logger;
    private readonly IAuditService _auditService;
    private readonly List<Teacher> _teachers; // In-memory storage for demo

    // Security configuration from FRS requirements
    private const int MAX_FAILED_ATTEMPTS = 3;
    private const int LOCKOUT_DURATION_MINUTES = 15;
    private const int PIN_EXPIRY_DAYS = 90;
    private const int PIN_LENGTH = 6;

    public TeacherPinService(ILogger<TeacherPinService> logger, IAuditService auditService)
    {
        _logger = logger;
        _auditService = auditService;
        _teachers = new List<Teacher>();
    }

    public async Task<Teacher> RegisterTeacherAsync(string name, string email, string pin)
    {
        try
        {
            // Validate PIN format
            var validationResult = await ValidatePinFormatAsync(pin);
            if (!validationResult.IsValid)
            {
                throw new ArgumentException($"Invalid PIN format: {string.Join(", ", validationResult.ValidationErrors)}");
            }

            // Check if teacher already exists
            var existingTeacher = _teachers.FirstOrDefault(t => t.Email == email && t.IsActive);
            if (existingTeacher != null)
            {
                throw new InvalidOperationException("Teacher with this email already exists");
            }

            var teacher = new Teacher
            {
                Id = _teachers.Count + 1,
                UserId = Guid.NewGuid().ToString(),
                Name = name,
                Email = email,
                PinHash = HashPin(pin),
                PinCreatedAt = DateTime.UtcNow,
                PinExpiresAt = DateTime.UtcNow.AddDays(PIN_EXPIRY_DAYS)
            };

            _teachers.Add(teacher);

            await _auditService.LogActionAsync(teacher.UserId, "Teacher Registered", 
                $"Teacher {name} registered with email {email}");

            _logger.LogInformation("Teacher registered: {Email} with user ID {UserId}", email, teacher.UserId);

            return teacher;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering teacher: {Email}", email);
            throw;
        }
    }

    public async Task<TeacherPinResponse> ValidatePinAsync(string userId, string pin)
    {
        try
        {
            var teacher = _teachers.FirstOrDefault(t => t.UserId == userId && t.IsActive);
            if (teacher == null)
            {
                await _auditService.LogActionAsync(userId, "PIN Validation Failed", "Teacher not found");
                return new TeacherPinResponse
                {
                    IsValid = false,
                    Message = "Teacher not found"
                };
            }

            // Check if account is locked
            if (teacher.LockedUntil.HasValue && teacher.LockedUntil > DateTime.UtcNow)
            {
                await _auditService.LogActionAsync(userId, "PIN Validation Failed", "Account is locked");
                return new TeacherPinResponse
                {
                    IsValid = false,
                    IsLocked = true,
                    LockedUntil = teacher.LockedUntil,
                    Message = $"Account is locked until {teacher.LockedUntil:yyyy-MM-dd HH:mm:ss}. Please try again later."
                };
            }

            // Check if PIN has expired
            if (DateTime.UtcNow > teacher.PinExpiresAt)
            {
                await _auditService.LogActionAsync(userId, "PIN Validation Failed", "PIN has expired");
                return new TeacherPinResponse
                {
                    IsValid = false,
                    RequiresReset = true,
                    Message = "Your PIN has expired. Please reset your PIN."
                };
            }

            // Validate PIN format first
            var formatValidation = await ValidatePinFormatAsync(pin);
            if (!formatValidation.IsValid)
            {
                await _auditService.LogActionAsync(userId, "PIN Validation Failed", 
                    $"Invalid PIN format: {string.Join(", ", formatValidation.ValidationErrors)}");
                return new TeacherPinResponse
                {
                    IsValid = false,
                    Message = $"Invalid PIN format: {string.Join(", ", formatValidation.ValidationErrors)}",
                    RemainingAttempts = MAX_FAILED_ATTEMPTS - teacher.FailedAttempts
                };
            }

            // Validate PIN
            if (VerifyPin(pin, teacher.PinHash))
            {
                // Reset failed attempts on successful validation
                teacher.FailedAttempts = 0;
                teacher.LockedUntil = null;

                await _auditService.LogActionAsync(userId, "PIN Validation Successful", 
                    $"Teacher {teacher.Name} successfully authenticated");

                _logger.LogInformation("PIN validation successful for teacher: {Email}", teacher.Email);

                return new TeacherPinResponse
                {
                    IsValid = true,
                    Message = "PIN validation successful. Redirecting to teacher dashboard..."
                };
            }
            else
            {
                // Increment failed attempts
                teacher.FailedAttempts++;

                // Check if account should be locked
                if (teacher.FailedAttempts >= MAX_FAILED_ATTEMPTS)
                {
                    teacher.LockedUntil = DateTime.UtcNow.AddMinutes(LOCKOUT_DURATION_MINUTES);
                    await _auditService.LogActionAsync(userId, "Account Locked", 
                        $"Account locked due to {MAX_FAILED_ATTEMPTS} failed PIN attempts");

                    _logger.LogWarning("Account locked for teacher: {Email} due to failed PIN attempts", teacher.Email);

                    return new TeacherPinResponse
                    {
                        IsValid = false,
                        IsLocked = true,
                        LockedUntil = teacher.LockedUntil,
                        Message = $"Account locked due to too many failed attempts. Please try again after {LOCKOUT_DURATION_MINUTES} minutes."
                    };
                }

                var remainingAttempts = MAX_FAILED_ATTEMPTS - teacher.FailedAttempts;
                await _auditService.LogActionAsync(userId, "PIN Validation Failed", 
                    $"Invalid PIN entered. {remainingAttempts} attempts remaining");

                _logger.LogWarning("PIN validation failed for teacher: {Email}. {RemainingAttempts} attempts remaining", 
                    teacher.Email, remainingAttempts);

                return new TeacherPinResponse
                {
                    IsValid = false,
                    RemainingAttempts = remainingAttempts,
                    Message = $"Invalid PIN. You have {remainingAttempts} attempt(s) remaining before account lockout."
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating PIN for user: {UserId}", userId);
            throw;
        }
    }

    public async Task<TeacherPinValidationResult> ValidatePinFormatAsync(string pin)
    {
        var result = new TeacherPinValidationResult { IsValid = true };

        try
        {
            // Check if PIN is null or empty
            if (string.IsNullOrEmpty(pin))
            {
                result.ValidationErrors.Add("PIN is required");
                result.IsValid = false;
            }
            else
            {
                // Check length - exactly 6 digits required
                if (pin.Length != PIN_LENGTH)
                {
                    result.ValidationErrors.Add($"PIN must be exactly {PIN_LENGTH} digits");
                    result.IsValid = false;
                }

                // Check if only numeric characters (0-9) are allowed
                if (!Regex.IsMatch(pin, @"^\d+$"))
                {
                    result.ValidationErrors.Add("PIN must contain only numeric characters (0-9)");
                    result.IsValid = false;
                }
            }

            if (result.IsValid)
            {
                result.Message = "PIN format is valid";
            }
            else
            {
                result.Message = "PIN format validation failed";
            }

            return await Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating PIN format");
            throw;
        }
    }

    public async Task<Teacher?> GetTeacherAsync(string userId)
    {
        try
        {
            var teacher = _teachers.FirstOrDefault(t => t.UserId == userId && t.IsActive);
            return await Task.FromResult(teacher);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving teacher: {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> ResetPinAsync(string userId, string newPin)
    {
        try
        {
            var teacher = _teachers.FirstOrDefault(t => t.UserId == userId && t.IsActive);
            if (teacher == null)
            {
                return false;
            }

            // Validate new PIN format
            var validationResult = await ValidatePinFormatAsync(newPin);
            if (!validationResult.IsValid)
            {
                throw new ArgumentException($"Invalid PIN format: {string.Join(", ", validationResult.ValidationErrors)}");
            }

            teacher.PinHash = HashPin(newPin);
            teacher.PinCreatedAt = DateTime.UtcNow;
            teacher.PinExpiresAt = DateTime.UtcNow.AddDays(PIN_EXPIRY_DAYS);
            teacher.FailedAttempts = 0;
            teacher.LockedUntil = null;

            await _auditService.LogActionAsync(userId, "PIN Reset", "Teacher PIN has been reset");

            _logger.LogInformation("PIN reset for teacher: {UserId}", userId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting PIN for user: {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> UnlockAccountAsync(string userId)
    {
        try
        {
            var teacher = _teachers.FirstOrDefault(t => t.UserId == userId && t.IsActive);
            if (teacher == null)
            {
                return false;
            }

            teacher.FailedAttempts = 0;
            teacher.LockedUntil = null;

            await _auditService.LogActionAsync(userId, "Account Unlocked", "Teacher account has been manually unlocked");

            _logger.LogInformation("Account unlocked for teacher: {UserId}", userId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unlocking account for user: {UserId}", userId);
            throw;
        }
    }

    private string HashPin(string pin)
    {
        // Use SHA256 for PIN hashing (in production, consider using bcrypt or similar)
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(pin + "TeacherPinSalt")); // Add salt
        return Convert.ToBase64String(hashedBytes);
    }

    private bool VerifyPin(string pin, string hashedPin)
    {
        var hashedInput = HashPin(pin);
        return hashedInput == hashedPin;
    }
}