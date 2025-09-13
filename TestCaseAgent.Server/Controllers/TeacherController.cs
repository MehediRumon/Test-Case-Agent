using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TestCaseAgent.Server.Models;
using TestCaseAgent.Server.Services;

namespace TestCaseAgent.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TeacherController : ControllerBase
{
    private readonly ITeacherPinService _teacherPinService;
    private readonly ILogger<TeacherController> _logger;

    public TeacherController(ITeacherPinService teacherPinService, ILogger<TeacherController> logger)
    {
        _teacherPinService = teacherPinService;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<ActionResult<Teacher>> RegisterTeacher([FromBody] TeacherRegistrationRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Name) || string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Pin))
            {
                return BadRequest("Name, email, and PIN are required");
            }

            var teacher = await _teacherPinService.RegisterTeacherAsync(request.Name, request.Email, request.Pin);
            
            // Don't return sensitive information
            var response = new
            {
                teacher.Id,
                teacher.UserId,
                teacher.Name,
                teacher.Email,
                teacher.IsActive,
                teacher.CreatedAt,
                PinExpiresAt = teacher.PinExpiresAt
            };

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid teacher registration request");
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Teacher registration conflict");
            return Conflict(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering teacher");
            return BadRequest($"Failed to register teacher: {ex.Message}");
        }
    }

    [HttpPost("validate-pin")]
    public async Task<ActionResult<TeacherPinResponse>> ValidatePin([FromBody] TeacherPinRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Pin))
            {
                return BadRequest("PIN is required");
            }

            // For demo purposes, get userId from query parameter or use demo teacher
            var userId = HttpContext.Request.Query["userId"].FirstOrDefault();
            if (string.IsNullOrEmpty(userId))
            {
                // Try to get from claims if authenticated
                userId = User.Identity?.Name ?? "demo-teacher";
            }

            var response = await _teacherPinService.ValidatePinAsync(userId, request.Pin);

            if (response.IsValid)
            {
                return Ok(response);
            }
            else if (response.IsLocked)
            {
                return StatusCode(423, response); // 423 Locked
            }
            else if (response.RequiresReset)
            {
                return StatusCode(422, response); // 422 Unprocessable Entity
            }
            else
            {
                return BadRequest(response);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating PIN");
            return BadRequest($"Failed to validate PIN: {ex.Message}");
        }
    }

    [HttpPost("validate-pin-format")]
    public async Task<ActionResult<TeacherPinValidationResult>> ValidatePinFormat([FromBody] TeacherPinRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Pin))
            {
                return BadRequest("PIN is required");
            }

            var result = await _teacherPinService.ValidatePinFormatAsync(request.Pin);
            
            if (result.IsValid)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating PIN format");
            return BadRequest($"Failed to validate PIN format: {ex.Message}");
        }
    }

    [HttpGet("{userId}")]
    public async Task<ActionResult<object>> GetTeacher(string userId)
    {
        try
        {
            var teacher = await _teacherPinService.GetTeacherAsync(userId);
            
            if (teacher == null)
            {
                return NotFound("Teacher not found");
            }

            // Don't return sensitive information
            var response = new
            {
                teacher.Id,
                teacher.UserId,
                teacher.Name,
                teacher.Email,
                teacher.IsActive,
                teacher.CreatedAt,
                PinExpiresAt = teacher.PinExpiresAt,
                teacher.FailedAttempts,
                IsLocked = teacher.LockedUntil.HasValue && teacher.LockedUntil > DateTime.UtcNow,
                teacher.LockedUntil
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving teacher: {UserId}", userId);
            return BadRequest($"Failed to retrieve teacher: {ex.Message}");
        }
    }

    [HttpPost("{userId}/reset-pin")]
    public async Task<ActionResult> ResetPin(string userId, [FromBody] TeacherPinRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Pin))
            {
                return BadRequest("New PIN is required");
            }

            var success = await _teacherPinService.ResetPinAsync(userId, request.Pin);
            
            if (success)
            {
                return Ok(new { message = "PIN has been reset successfully" });
            }
            else
            {
                return NotFound("Teacher not found");
            }
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid PIN reset request for user: {UserId}", userId);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting PIN for user: {UserId}", userId);
            return BadRequest($"Failed to reset PIN: {ex.Message}");
        }
    }

    [HttpPost("{userId}/unlock")]
    public async Task<ActionResult> UnlockAccount(string userId)
    {
        try
        {
            var success = await _teacherPinService.UnlockAccountAsync(userId);
            
            if (success)
            {
                return Ok(new { message = "Account has been unlocked successfully" });
            }
            else
            {
                return NotFound("Teacher not found");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unlocking account for user: {UserId}", userId);
            return BadRequest($"Failed to unlock account: {ex.Message}");
        }
    }

    [HttpGet("demo/create-sample")]
    public async Task<ActionResult<object>> CreateSampleTeacher()
    {
        try
        {
            // Create a sample teacher for demo purposes
            var teacher = await _teacherPinService.RegisterTeacherAsync(
                "Demo Teacher", 
                "demo.teacher@example.com", 
                "123456");

            var response = new
            {
                teacher.Id,
                teacher.UserId,
                teacher.Name,
                teacher.Email,
                teacher.IsActive,
                teacher.CreatedAt,
                PinExpiresAt = teacher.PinExpiresAt,
                Message = "Sample teacher created with PIN: 123456"
            };

            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Sample teacher already exists");
            
            // Try to find the existing demo teacher
            var existingTeachers = await Task.FromResult(new List<object>());
            return Ok(new { message = "Demo teacher may already exist", error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating sample teacher");
            return BadRequest($"Failed to create sample teacher: {ex.Message}");
        }
    }
}