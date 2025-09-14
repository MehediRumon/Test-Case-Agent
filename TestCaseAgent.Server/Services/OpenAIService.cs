using System.Text;
using System.Text.Json;
using TestCaseAgent.Server.Models;

namespace TestCaseAgent.Server.Services;

public interface IOpenAIService
{
    Task<string> GenerateResponseAsync(string prompt, string context = "");
    Task<AgentResponse> AnswerQuestionAsync(string question, string frsContent);
    Task<List<TestCase>> GenerateTestCasesAsync(string requirementText, string requirementId, string userId);
    Task<TestCase> GenerateTestCaseFromPromptAsync(string prompt, string frsContent, string userId);
}

public class OpenAIService : IOpenAIService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenAIService> _logger;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly int _maxTokens;
    private readonly float _temperature;

    public OpenAIService(HttpClient httpClient, IConfiguration configuration, ILogger<OpenAIService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        
        // Try multiple configuration sources for API key
        _apiKey = GetApiKeyFromConfiguration(configuration);
        
        // Validate API key is properly configured
        ValidateApiKey(_apiKey);

        _model = configuration["OpenAI:Model"] ?? "gpt-4o-mini";
        _maxTokens = int.Parse(configuration["OpenAI:MaxTokens"] ?? "2000");
        _temperature = float.Parse(configuration["OpenAI:Temperature"] ?? "0.7");

        _httpClient.BaseAddress = new Uri("https://api.openai.com/");
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
        
        _logger.LogInformation("OpenAI service initialized with model: {Model}", _model);
    }
    
    private string GetApiKeyFromConfiguration(IConfiguration configuration)
    {
        // Try different configuration sources in order of preference
        var apiKey = configuration["OpenAI:ApiKey"] ??
                    configuration["OPENAI_API_KEY"] ??
                    Environment.GetEnvironmentVariable("OPENAI_API_KEY") ??
                    "";
                    
        _logger.LogDebug("OpenAI API key source: {HasKey}", !string.IsNullOrEmpty(apiKey) ? "Found" : "Not found");
        return apiKey;
    }
    
    private void ValidateApiKey(string apiKey)
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            var errorMessage = @"OpenAI API key is not configured. Please configure it using one of these methods:

1. Add to appsettings.json:
   ""OpenAI"": {
     ""ApiKey"": ""sk-your-actual-openai-api-key-here""
   }

2. Set environment variable:
   OPENAI_API_KEY=sk-your-actual-openai-api-key-here

3. Use .NET user secrets:
   dotnet user-secrets set ""OpenAI:ApiKey"" ""sk-your-actual-openai-api-key-here""

Get your API key from: https://platform.openai.com/account/api-keys";

            _logger.LogError("OpenAI API key validation failed: {Error}", errorMessage);
            throw new InvalidOperationException(errorMessage);
        }
        
        // Check for placeholder values
        var placeholderValues = new[] { "your-openai-api-key", "your-openai-api-key-here", "sk-your-key-here" };
        if (placeholderValues.Any(placeholder => apiKey.Contains(placeholder, StringComparison.OrdinalIgnoreCase)))
        {
            var errorMessage = $@"OpenAI API key appears to be a placeholder value: {MaskApiKey(apiKey)}

Please replace it with your actual OpenAI API key from: https://platform.openai.com/account/api-keys

Valid OpenAI API keys start with 'sk-' followed by a long string of characters.";

            _logger.LogError("OpenAI API key validation failed - placeholder detected: {MaskedKey}", MaskApiKey(apiKey));
            throw new InvalidOperationException(errorMessage);
        }
        
        // Check basic format
        if (!apiKey.StartsWith("sk-") || apiKey.Length < 20)
        {
            var errorMessage = $@"OpenAI API key format appears invalid: {MaskApiKey(apiKey)}

Valid OpenAI API keys:
- Start with 'sk-'
- Are typically 51 characters long
- Contain only alphanumeric characters and hyphens

Please verify your API key from: https://platform.openai.com/account/api-keys";

            _logger.LogError("OpenAI API key validation failed - invalid format: {MaskedKey}", MaskApiKey(apiKey));
            throw new InvalidOperationException(errorMessage);
        }
        
        _logger.LogInformation("OpenAI API key validation passed: {MaskedKey}", MaskApiKey(apiKey));
    }
    
    private string MaskApiKey(string apiKey)
    {
        if (string.IsNullOrEmpty(apiKey))
            return "[empty]";
            
        if (apiKey.Length <= 8)
            return "***";
            
        return apiKey.Substring(0, 6) + "***" + apiKey.Substring(Math.Max(6, apiKey.Length - 4));
    }

    public async Task<string> GenerateResponseAsync(string prompt, string context = "")
    {
        try
        {
            var systemMessage = !string.IsNullOrEmpty(context) 
                ? $"You are a helpful assistant with expertise in software testing and requirements analysis. Use the following context to answer questions: {context}"
                : "You are a helpful assistant with expertise in software testing and requirements analysis.";

            var requestBody = new
            {
                model = _model,
                messages = new[]
                {
                    new { role = "system", content = systemMessage },
                    new { role = "user", content = prompt }
                },
                max_tokens = _maxTokens,
                temperature = _temperature
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("v1/chat/completions", content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("OpenAI API error: {StatusCode} - {Content}", response.StatusCode, errorContent);
                
                // Provide specific error messages based on status code
                var errorMessage = response.StatusCode switch
                {
                    System.Net.HttpStatusCode.Unauthorized => $"OpenAI API authentication failed. Please verify your API key is correct and active.\nAPI Key (masked): {MaskApiKey(_apiKey)}\nError details: {errorContent}",
                    System.Net.HttpStatusCode.PaymentRequired => "OpenAI API billing issue. Please check your account billing and usage limits.",
                    System.Net.HttpStatusCode.TooManyRequests => "OpenAI API rate limit exceeded. Please try again later.",
                    System.Net.HttpStatusCode.InternalServerError => "OpenAI API server error. Please try again later.",
                    _ => $"OpenAI API error: {response.StatusCode} - {errorContent}"
                };
                
                throw new HttpRequestException(errorMessage);
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<OpenAIResponse>(responseContent);

            if (result?.Choices?.Length > 0)
            {
                var responseText = result.Choices[0].Message.Content;
                _logger.LogInformation("Generated OpenAI response for prompt: {Prompt}", prompt.Take(100) + "...");
                return responseText;
            }

            throw new InvalidOperationException("No response from OpenAI");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating OpenAI response for prompt: {Prompt}", prompt);
            throw;
        }
    }

    public async Task<AgentResponse> AnswerQuestionAsync(string question, string frsContent)
    {
        try
        {
            _logger.LogInformation("Processing question with OpenAI: {Question}", question);

            var prompt = $@"
Based on the Functional Requirements Specification (FRS) document provided below, please answer the user's question.

FRS Document:
{frsContent}

User Question: {question}

Please provide a comprehensive answer based on the FRS content. If the information is not available in the FRS, please clearly state that.
Also, provide relevant references or sections from the FRS that support your answer.

Format your response as:
1. Direct answer to the question
2. Supporting references from the FRS
";

            var answer = await GenerateResponseAsync(prompt);
            
            // Extract references from the FRS based on the question
            var references = ExtractRelevantSections(question, frsContent);

            return new AgentResponse
            {
                Answer = answer,
                References = references
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing question with OpenAI: {Question}", question);
            throw;
        }
    }

    public async Task<List<TestCase>> GenerateTestCasesAsync(string requirementText, string requirementId, string userId)
    {
        try
        {
            _logger.LogInformation("Generating test cases with OpenAI for requirement: {RequirementId}", requirementId);

            var prompt = $@"
Generate comprehensive test cases for the following software requirement:

Requirement ID: {requirementId}
Requirement Text: {requirementText}

Please generate test cases covering:
1. Positive test scenarios (happy path)
2. Negative test scenarios (error handling)
3. Boundary value testing (if applicable)
4. Edge cases

For each test case, provide:
- Title
- Description
- Preconditions
- Test Steps (numbered)
- Expected Results
- Priority (High/Medium/Low)

Return the response in a structured format that clearly separates each test case.
";

            var response = await GenerateResponseAsync(prompt);
            
            // Parse the OpenAI response and convert to TestCase objects
            return ParseTestCasesFromResponse(response, requirementId, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating test cases with OpenAI for requirement: {RequirementId}", requirementId);
            throw;
        }
    }

    public async Task<TestCase> GenerateTestCaseFromPromptAsync(string prompt, string frsContent, string userId)
    {
        try
        {
            _logger.LogInformation("Generating test case from prompt with OpenAI: {Prompt}", prompt);

            var systemPrompt = $@"
Generate a detailed test case based on the user's request and the FRS document provided.

FRS Document Context:
{frsContent}

User Request: {prompt}

Please create a single, comprehensive test case that addresses the user's request. Include:
- Title
- Description
- Preconditions
- Detailed test steps
- Expected results
- Priority level

Ensure the test case is realistic and executable based on the requirements in the FRS.
";

            var response = await GenerateResponseAsync(systemPrompt);
            
            // Parse the response and create a TestCase object
            return ParseSingleTestCaseFromResponse(response, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating test case from prompt with OpenAI: {Prompt}", prompt);
            throw;
        }
    }

    private List<string> ExtractRelevantSections(string question, string frsContent)
    {
        // Simple keyword-based extraction for references
        var keywords = ExtractKeywords(question);
        var sections = new List<string>();

        foreach (var keyword in keywords.Take(3)) // Limit to top 3 keywords
        {
            var sentences = frsContent.Split('.', StringSplitOptions.RemoveEmptyEntries);
            foreach (var sentence in sentences)
            {
                if (sentence.ToLower().Contains(keyword.ToLower()) && sentence.Length > 50)
                {
                    sections.Add(sentence.Trim() + ".");
                    if (sections.Count >= 5) break; // Limit to 5 references
                }
            }
            if (sections.Count >= 5) break;
        }

        return sections;
    }

    private List<string> ExtractKeywords(string text)
    {
        var commonWords = new HashSet<string> { "the", "a", "an", "and", "or", "but", "in", "on", "at", "to", "for", "of", "with", "by", "what", "how", "when", "where", "why", "which", "who" };
        
        var words = System.Text.RegularExpressions.Regex.Replace(text.ToLower(), @"[^\w\s]", "")
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 3 && !commonWords.Contains(w))
            .ToList();

        return words;
    }

    private List<TestCase> ParseTestCasesFromResponse(string response, string requirementId, string userId)
    {
        var testCases = new List<TestCase>();
        
        try
        {
            // Simple parsing - in a production system, you might want more sophisticated parsing
            var sections = response.Split(new[] { "Test Case", "TEST CASE" }, StringSplitOptions.RemoveEmptyEntries);
            
            for (int i = 1; i < sections.Length; i++) // Skip first empty section
            {
                var section = sections[i].Trim();
                var testCase = ParseSingleTestCaseSection(section, requirementId, userId, i);
                if (testCase != null)
                {
                    testCases.Add(testCase);
                }
            }

            // If no structured test cases found, create a fallback
            if (testCases.Count == 0)
            {
                testCases.Add(new TestCase
                {
                    Title = $"Generated Test Case for {requirementId}",
                    Description = "Test case generated from OpenAI response",
                    Preconditions = "System is ready for testing",
                    TestSteps = response.Length > 500 ? response.Substring(0, 500) + "..." : response,
                    ExpectedResults = "System should behave as expected per the requirement",
                    Priority = TestCasePriority.Medium,
                    RequirementReference = requirementId,
                    CreatedBy = userId
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error parsing test cases from OpenAI response, creating fallback");
            
            // Fallback test case
            testCases.Add(new TestCase
            {
                Title = $"Generated Test Case for {requirementId}",
                Description = "Test case generated from OpenAI response",
                Preconditions = "System is ready for testing",
                TestSteps = "1. Review the OpenAI generated content\n2. Execute the test scenarios\n3. Verify results",
                ExpectedResults = "System should behave as expected per the requirement",
                Priority = TestCasePriority.Medium,
                RequirementReference = requirementId,
                CreatedBy = userId
            });
        }

        return testCases;
    }

    private TestCase? ParseSingleTestCaseSection(string section, string requirementId, string userId, int index)
    {
        try
        {
            // Extract title (usually the first line)
            var lines = section.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var title = lines.Length > 0 ? lines[0].Trim() : $"Test Case {index} for {requirementId}";
            
            // Clean up title
            title = title.Replace(":", "").Trim();
            if (title.Length > 100) title = title.Substring(0, 100) + "...";

            return new TestCase
            {
                Title = title,
                Description = section.Length > 200 ? section.Substring(0, 200) + "..." : section,
                Preconditions = "System is ready for testing",
                TestSteps = ExtractTestSteps(section),
                ExpectedResults = ExtractExpectedResults(section),
                Priority = ExtractPriority(section),
                RequirementReference = requirementId,
                CreatedBy = userId
            };
        }
        catch
        {
            return null;
        }
    }

    private TestCase ParseSingleTestCaseFromResponse(string response, string userId)
    {
        return new TestCase
        {
            Title = ExtractTitle(response) ?? "Generated Test Case",
            Description = ExtractDescription(response) ?? response.Take(200) + "...",
            Preconditions = ExtractPreconditions(response) ?? "System is ready for testing",
            TestSteps = ExtractTestSteps(response),
            ExpectedResults = ExtractExpectedResults(response),
            Priority = ExtractPriority(response),
            RequirementReference = "Custom",
            CreatedBy = userId
        };
    }

    private string? ExtractTitle(string text)
    {
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var titleLine = lines.FirstOrDefault(l => l.ToLower().Contains("title") || l.StartsWith("##"));
        return titleLine?.Replace("Title:", "").Replace("##", "").Trim();
    }

    private string? ExtractDescription(string text)
    {
        var descMatch = System.Text.RegularExpressions.Regex.Match(text, @"Description[:\s]+(.*?)(?=\n.*?:|\n\n|$)", System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);
        return descMatch.Success ? descMatch.Groups[1].Value.Trim() : null;
    }

    private string? ExtractPreconditions(string text)
    {
        var preMatch = System.Text.RegularExpressions.Regex.Match(text, @"Preconditions?[:\s]+(.*?)(?=\n.*?:|\n\n|$)", System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);
        return preMatch.Success ? preMatch.Groups[1].Value.Trim() : null;
    }

    private string ExtractTestSteps(string text)
    {
        var stepsMatch = System.Text.RegularExpressions.Regex.Match(text, @"(?:Test\s+)?Steps?[:\s]+(.*?)(?=\n.*?(?:Expected|Priority|$))", System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);
        if (stepsMatch.Success)
        {
            return stepsMatch.Groups[1].Value.Trim();
        }

        // Fallback: look for numbered steps
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var stepLines = lines.Where(l => System.Text.RegularExpressions.Regex.IsMatch(l, @"^\d+\.")).ToList();
        
        return stepLines.Any() ? string.Join("\n", stepLines) : "1. Execute the test scenario\n2. Verify the results";
    }

    private string ExtractExpectedResults(string text)
    {
        var resultsMatch = System.Text.RegularExpressions.Regex.Match(text, @"Expected\s+Results?[:\s]+(.*?)(?=\n.*?:|\n\n|$)", System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);
        return resultsMatch.Success ? resultsMatch.Groups[1].Value.Trim() : "System should behave as expected";
    }

    private TestCasePriority ExtractPriority(string text)
    {
        if (text.ToLower().Contains("high"))
            return TestCasePriority.High;
        if (text.ToLower().Contains("low"))
            return TestCasePriority.Low;
        return TestCasePriority.Medium;
    }
}

// OpenAI API response models
public class OpenAIResponse
{
    public Choice[]? Choices { get; set; }
}

public class Choice
{
    public Message Message { get; set; } = new();
}

public class Message
{
    public string Content { get; set; } = "";
}