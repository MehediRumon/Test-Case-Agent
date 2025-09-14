using Microsoft.Extensions.Options;

namespace TestCaseAgent.Server.Services;

public class ConfigurationValidationService : IHostedService
{
    private readonly ILogger<ConfigurationValidationService> _logger;
    private readonly IConfiguration _configuration;

    public ConfigurationValidationService(ILogger<ConfigurationValidationService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        ValidateConfiguration();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private void ValidateConfiguration()
    {
        _logger.LogInformation("Starting configuration validation...");

        // Validate OpenAI configuration
        ValidateOpenAIConfiguration();

        // Validate Google OAuth configuration
        ValidateGoogleOAuthConfiguration();

        _logger.LogInformation("Configuration validation completed");
    }

    private void ValidateOpenAIConfiguration()
    {
        var apiKey = _configuration["OpenAI:ApiKey"] ??
                    _configuration["OPENAI_API_KEY"] ??
                    Environment.GetEnvironmentVariable("OPENAI_API_KEY");

        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning(@"⚠️  OpenAI API key is not configured. The AI features will not work.

To configure OpenAI API key, choose one of these methods:

1. Add to appsettings.json:
   ""OpenAI"": {{
     ""ApiKey"": ""sk-your-actual-openai-api-key-here""
   }}

2. Set environment variable:
   OPENAI_API_KEY=sk-your-actual-openai-api-key-here

3. Use .NET user secrets (for development):
   dotnet user-secrets set ""OpenAI:ApiKey"" ""sk-your-actual-openai-api-key-here""

Get your API key from: https://platform.openai.com/account/api-keys");
            return;
        }

        // Check for placeholder values
        var placeholderValues = new[] { "your-openai-api-key", "your-openai-api-key-here", "sk-your-key-here" };
        if (placeholderValues.Any(placeholder => apiKey.Contains(placeholder, StringComparison.OrdinalIgnoreCase)))
        {
            _logger.LogWarning(@"⚠️  OpenAI API key appears to be a placeholder value: {ApiKey}

Please replace it with your actual OpenAI API key from: https://platform.openai.com/account/api-keys

Valid OpenAI API keys start with 'sk-' followed by a long string of characters.", MaskApiKey(apiKey));
            return;
        }

        // Check basic format
        if (!apiKey.StartsWith("sk-") || apiKey.Length < 20)
        {
            _logger.LogWarning(@"⚠️  OpenAI API key format appears invalid: {ApiKey}

Valid OpenAI API keys:
- Start with 'sk-'
- Are typically 51 characters long
- Contain only alphanumeric characters and hyphens

Please verify your API key from: https://platform.openai.com/account/api-keys", MaskApiKey(apiKey));
            return;
        }

        _logger.LogInformation("✅ OpenAI API key configuration looks valid: {ApiKey}", MaskApiKey(apiKey));
    }

    private void ValidateGoogleOAuthConfiguration()
    {
        var clientId = _configuration["Authentication:Google:ClientId"];
        var clientSecret = _configuration["Authentication:Google:ClientSecret"];

        if (string.IsNullOrEmpty(clientId) || clientId.Contains("your-google-client-id"))
        {
            _logger.LogWarning(@"⚠️  Google OAuth Client ID is not configured or is a placeholder.

To configure Google OAuth:
1. Go to Google Cloud Console: https://console.cloud.google.com/
2. Enable Google Docs API and Google Sheets API
3. Create OAuth 2.0 credentials
4. Add authorized redirect URIs:
   - https://localhost:7000/auth/callback
   - http://localhost:5000/auth/callback
5. Update appsettings.json with your Client ID and Client Secret

See OAUTH_SETUP.md for detailed instructions.");
        }
        else
        {
            _logger.LogInformation("✅ Google OAuth Client ID is configured");
        }

        if (string.IsNullOrEmpty(clientSecret) || clientSecret.Contains("your-google-client-secret"))
        {
            _logger.LogWarning("⚠️  Google OAuth Client Secret is not configured or is a placeholder");
        }
        else
        {
            _logger.LogInformation("✅ Google OAuth Client Secret is configured");
        }
    }

    private string MaskApiKey(string apiKey)
    {
        if (string.IsNullOrEmpty(apiKey))
            return "[empty]";

        if (apiKey.Length <= 8)
            return "***";

        return apiKey.Substring(0, 6) + "***" + apiKey.Substring(Math.Max(6, apiKey.Length - 4));
    }
}