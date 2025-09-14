using Microsoft.Extensions.Options;

namespace TestCaseAgent.Server.Services;

public class ConfigurationValidationService : IHostedService
{
    private readonly ILogger<ConfigurationValidationService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;

    public ConfigurationValidationService(ILogger<ConfigurationValidationService> logger, IConfiguration configuration, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _configuration = configuration;
        _serviceProvider = serviceProvider;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await ValidateConfigurationAsync();
        return;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private async Task ValidateConfigurationAsync()
    {
        _logger.LogInformation("Starting configuration validation...");

        // Validate OpenAI configuration
        await ValidateOpenAIConfigurationAsync();

        // Validate Google OAuth configuration
        ValidateGoogleOAuthConfiguration();

        _logger.LogInformation("Configuration validation completed");
    }

    private async Task ValidateOpenAIConfigurationAsync()
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
        var placeholderValues = new[] { 
            "your-openai-api-key", 
            "your-openai-api-key-here", 
            "sk-your-key-here",
            "sk-your-actual-openai-api-key-here"
        };
        if (placeholderValues.Any(placeholder => apiKey.Contains(placeholder, StringComparison.OrdinalIgnoreCase)))
        {
            _logger.LogWarning(@"⚠️  OpenAI API key appears to be a placeholder value: {ApiKey}

Please replace it with your actual OpenAI API key from: https://platform.openai.com/account/api-keys

Valid OpenAI API keys start with 'sk-' or 'sk-proj-' followed by a long string of characters.", MaskApiKey(apiKey));
            return;
        }

        // Check basic format - support both traditional and project-scoped API keys
        var validPrefixes = new[] { "sk-", "sk-proj-" };
        if (!validPrefixes.Any(prefix => apiKey.StartsWith(prefix)) || apiKey.Length < 20)
        {
            _logger.LogWarning(@"⚠️  OpenAI API key format appears invalid: {ApiKey}

Valid OpenAI API keys:
- Start with 'sk-' (traditional) or 'sk-proj-' (project-scoped)
- Are typically 51-120 characters long
- Contain only alphanumeric characters and hyphens

Please verify your API key from: https://platform.openai.com/account/api-keys", MaskApiKey(apiKey));
            return;
        }

        // Format looks good, try to validate with OpenAI if service is available
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var openAIService = scope.ServiceProvider.GetService<IOpenAIService>();
            
            if (openAIService != null)
            {
                _logger.LogInformation("🔍 Testing API key with OpenAI...");
                var validationResult = await openAIService.ValidateApiKeyAsync();
                
                if (validationResult.IsValid)
                {
                    _logger.LogInformation("✅ OpenAI API key validated successfully: {ApiKey}", MaskApiKey(apiKey));
                    if (apiKey.StartsWith("sk-proj-"))
                    {
                        _logger.LogInformation("ℹ️  Project-scoped API key detected. Ensure project has access to required models.");
                    }
                }
                else
                {
                    _logger.LogWarning(@"❌ OpenAI API key validation failed: {ApiKey}

Error: {ErrorMessage}
Recommendation: {Recommendation}

🔧 Common Solutions:
1. Verify the key is active at: https://platform.openai.com/account/api-keys
2. Check billing status: https://platform.openai.com/account/billing
3. For project keys: verify project permissions and model access
4. Try regenerating the API key if it might be compromised", 
                        MaskApiKey(apiKey), 
                        validationResult.ErrorMessage, 
                        validationResult.Recommendation);
                }
            }
            else
            {
                _logger.LogInformation("✅ OpenAI API key configuration looks valid: {ApiKey} (service validation skipped)", MaskApiKey(apiKey));
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️  Could not validate OpenAI API key: {ApiKey}. Will proceed with basic format validation only.", MaskApiKey(apiKey));
            _logger.LogInformation("✅ OpenAI API key format is valid: {ApiKey}", MaskApiKey(apiKey));
        }
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