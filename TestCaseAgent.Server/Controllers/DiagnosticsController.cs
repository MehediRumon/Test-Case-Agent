using Microsoft.AspNetCore.Mvc;
using TestCaseAgent.Server.Services;

namespace TestCaseAgent.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DiagnosticsController : ControllerBase
{
    private readonly IOpenAIService _openAIService;
    private readonly ILogger<DiagnosticsController> _logger;
    private readonly IConfiguration _configuration;

    public DiagnosticsController(IOpenAIService openAIService, ILogger<DiagnosticsController> logger, IConfiguration configuration)
    {
        _openAIService = openAIService;
        _logger = logger;
        _configuration = configuration;
    }

    [HttpGet("openai-status")]
    public async Task<IActionResult> GetOpenAIStatus()
    {
        try
        {
            var validationResult = await _openAIService.ValidateApiKeyAsync();
            
            var response = new
            {
                IsConfigured = !string.IsNullOrEmpty(GetApiKey()),
                IsValid = validationResult.IsValid,
                ErrorMessage = validationResult.ErrorMessage,
                Recommendation = validationResult.Recommendation,
                KeyType = GetApiKeyType(),
                Model = _configuration["OpenAI:Model"] ?? "gpt-4o-mini",
                Timestamp = DateTime.UtcNow
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking OpenAI status");
            return StatusCode(500, new { error = "Failed to check OpenAI status", message = ex.Message });
        }
    }

    [HttpPost("test-openai")]
    public async Task<IActionResult> TestOpenAI([FromBody] string prompt = "Hello, this is a test.")
    {
        try
        {
            var response = await _openAIService.GenerateResponseAsync(prompt);
            return Ok(new { success = true, response = response });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing OpenAI");
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    [HttpGet("configuration-info")]
    public IActionResult GetConfigurationInfo()
    {
        var apiKey = GetApiKey();
        
        var info = new
        {
            OpenAI = new
            {
                HasApiKey = !string.IsNullOrEmpty(apiKey),
                KeyType = GetApiKeyType(),
                Model = _configuration["OpenAI:Model"] ?? "gpt-4o-mini",
                MaxTokens = _configuration["OpenAI:MaxTokens"] ?? "2000",
                Temperature = _configuration["OpenAI:Temperature"] ?? "0.7",
                ConfigurationSource = GetConfigurationSource()
            },
            Google = new
            {
                HasClientId = !string.IsNullOrEmpty(_configuration["Authentication:Google:ClientId"]) && 
                             !_configuration["Authentication:Google:ClientId"]!.Contains("your-google-client-id"),
                HasClientSecret = !string.IsNullOrEmpty(_configuration["Authentication:Google:ClientSecret"]) && 
                                 !_configuration["Authentication:Google:ClientSecret"]!.Contains("your-google-client-secret")
            },
            Timestamp = DateTime.UtcNow
        };

        return Ok(info);
    }

    private string? GetApiKey()
    {
        return _configuration["OpenAI:ApiKey"] ??
               _configuration["OPENAI_API_KEY"] ??
               Environment.GetEnvironmentVariable("OPENAI_API_KEY");
    }

    private string GetApiKeyType()
    {
        var apiKey = GetApiKey();
        if (string.IsNullOrEmpty(apiKey))
            return "Not configured";
        
        if (apiKey.StartsWith("sk-proj-"))
            return "Project-scoped";
        if (apiKey.StartsWith("sk-"))
            return "Traditional";
        
        return "Unknown format";
    }

    private string GetConfigurationSource()
    {
        if (!string.IsNullOrEmpty(_configuration["OpenAI:ApiKey"]))
            return "appsettings.json";
        if (!string.IsNullOrEmpty(_configuration["OPENAI_API_KEY"]))
            return "Configuration provider";
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("OPENAI_API_KEY")))
            return "Environment variable";
        
        return "Not found";
    }
}