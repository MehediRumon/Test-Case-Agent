using System.Text.Json;
using System.Text.RegularExpressions;
using TestCaseAgent.Server.Models;

namespace TestCaseAgent.Server.Services;

public interface IIntelligentAgentService
{
    Task<AgentResponse> AnswerQuestionAsync(string question, string frsContent);
    Task<List<TestCase>> GenerateTestCasesAsync(string requirementText, string requirementId, string userId);
    Task<TestCase> GenerateTestCaseFromPromptAsync(string prompt, string frsContent, string userId);
}

public class IntelligentAgentService : IIntelligentAgentService
{
    private readonly ILogger<IntelligentAgentService> _logger;
    private readonly IOpenAIService _openAIService;

    public IntelligentAgentService(ILogger<IntelligentAgentService> logger, IOpenAIService openAIService)
    {
        _logger = logger;
        _openAIService = openAIService;
    }

    public async Task<AgentResponse> AnswerQuestionAsync(string question, string frsContent)
    {
        try
        {
            _logger.LogInformation("Processing question: {Question}", question);

            // Use OpenAI service for intelligent responses
            return await _openAIService.AnswerQuestionAsync(question, frsContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing question: {Question}", question);
            
            // Fallback to basic processing if OpenAI fails
            _logger.LogWarning("Falling back to basic question processing");
            return await ProcessQuestionBasic(question, frsContent);
        }
    }

    public async Task<List<TestCase>> GenerateTestCasesAsync(string requirementText, string requirementId, string userId)
    {
        try
        {
            _logger.LogInformation("Generating test cases for requirement: {RequirementId}", requirementId);

            // Use OpenAI service for intelligent test case generation
            return await _openAIService.GenerateTestCasesAsync(requirementText, requirementId, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating test cases for requirement: {RequirementId}", requirementId);
            
            // Fallback to basic generation if OpenAI fails
            _logger.LogWarning("Falling back to basic test case generation");
            return await GenerateTestCasesBasic(requirementText, requirementId, userId);
        }
    }

    public async Task<TestCase> GenerateTestCaseFromPromptAsync(string prompt, string frsContent, string userId)
    {
        try
        {
            _logger.LogInformation("Generating test case from prompt: {Prompt}", prompt);

            // Use OpenAI service for intelligent test case generation
            return await _openAIService.GenerateTestCaseFromPromptAsync(prompt, frsContent, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating test case from prompt: {Prompt}", prompt);
            
            // Fallback to basic generation if OpenAI fails
            _logger.LogWarning("Falling back to basic test case generation from prompt");
            return await CreateTestCaseFromContentBasic(prompt, frsContent, userId);
        }
    }

    // Fallback methods for when OpenAI is not available
    private async Task<AgentResponse> ProcessQuestionBasic(string question, string frsContent)
    {
        // Basic question answering logic based on FRS content
        var answer = await ProcessQuestionAgainstFRS(question, frsContent);
        var references = ExtractRelevantSections(question, frsContent);

        return new AgentResponse
        {
            Answer = answer,
            References = references
        };
    }

    private async Task<List<TestCase>> GenerateTestCasesBasic(string requirementText, string requirementId, string userId)
    {
        var testCases = new List<TestCase>();

        // Generate positive test cases
        var positiveCase = await GeneratePositiveTestCase(requirementText, requirementId, userId);
        testCases.Add(positiveCase);

        // Generate negative test cases
        var negativeCase = await GenerateNegativeTestCase(requirementText, requirementId, userId);
        testCases.Add(negativeCase);

        // Generate boundary test cases if applicable
        if (ContainsBoundaryConditions(requirementText))
        {
            var boundaryCase = await GenerateBoundaryTestCase(requirementText, requirementId, userId);
            testCases.Add(boundaryCase);
        }

        return testCases;
    }

    private async Task<TestCase> CreateTestCaseFromContentBasic(string prompt, string frsContent, string userId)
    {
        var relevantContent = ExtractRelevantContent(prompt, frsContent);
        return await CreateTestCaseFromContent(prompt, relevantContent, userId);
    }

    private async Task<string> ProcessQuestionAgainstFRS(string question, string frsContent)
    {
        // Simple keyword-based matching and context extraction
        var keywords = ExtractKeywords(question);
        var relevantSections = new List<string>();

        foreach (var keyword in keywords)
        {
            var sections = FindSectionsContaining(keyword, frsContent);
            relevantSections.AddRange(sections);
        }

        if (!relevantSections.Any())
        {
            return "I couldn't find specific information about your question in the FRS document. Please try rephrasing your question or provide more specific terms.";
        }

        var answer = $"Based on the FRS document, here's what I found:\n\n{string.Join("\n\n", relevantSections.Take(3))}";
        
        return await Task.FromResult(answer);
    }

    private List<string> ExtractRelevantSections(string question, string frsContent)
    {
        var keywords = ExtractKeywords(question);
        var sections = new List<string>();

        foreach (var keyword in keywords)
        {
            var foundSections = FindSectionsContaining(keyword, frsContent);
            sections.AddRange(foundSections.Take(2)); // Limit to 2 sections per keyword
        }

        return sections.Take(5).ToList(); // Limit total references
    }

    private List<string> ExtractKeywords(string text)
    {
        // Remove common words and extract meaningful keywords
        var commonWords = new HashSet<string> { "the", "a", "an", "and", "or", "but", "in", "on", "at", "to", "for", "of", "with", "by", "what", "how", "when", "where", "why", "which", "who" };
        
        var words = Regex.Replace(text.ToLower(), @"[^\w\s]", "")
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 3 && !commonWords.Contains(w))
            .ToList();

        return words;
    }

    private List<string> FindSectionsContaining(string keyword, string content)
    {
        var sections = new List<string>();
        var sentences = content.Split('.', StringSplitOptions.RemoveEmptyEntries);

        foreach (var sentence in sentences)
        {
            if (sentence.ToLower().Contains(keyword.ToLower()) && sentence.Length > 50)
            {
                sections.Add(sentence.Trim() + ".");
            }
        }

        return sections;
    }

    private async Task<TestCase> GeneratePositiveTestCase(string requirementText, string requirementId, string userId)
    {
        var title = $"Positive Test Case for {requirementId}";
        var description = $"Verify that the system correctly implements the requirement: {requirementText.Take(100)}...";
        
        return await Task.FromResult(new TestCase
        {
            Title = title,
            Description = description,
            Preconditions = "System is in a valid state and all prerequisites are met",
            TestSteps = GeneratePositiveTestSteps(requirementText),
            ExpectedResults = "The system should behave as specified in the requirement",
            Priority = TestCasePriority.High,
            RequirementReference = requirementId,
            CreatedBy = userId
        });
    }

    private async Task<TestCase> GenerateNegativeTestCase(string requirementText, string requirementId, string userId)
    {
        var title = $"Negative Test Case for {requirementId}";
        var description = $"Verify that the system handles invalid inputs gracefully for requirement: {requirementText.Take(100)}...";
        
        return await Task.FromResult(new TestCase
        {
            Title = title,
            Description = description,
            Preconditions = "System is in a valid state",
            TestSteps = GenerateNegativeTestSteps(requirementText),
            ExpectedResults = "The system should handle invalid inputs appropriately and show proper error messages",
            Priority = TestCasePriority.Medium,
            RequirementReference = requirementId,
            CreatedBy = userId
        });
    }

    private async Task<TestCase> GenerateBoundaryTestCase(string requirementText, string requirementId, string userId)
    {
        var title = $"Boundary Test Case for {requirementId}";
        var description = $"Verify boundary conditions for requirement: {requirementText.Take(100)}...";
        
        return await Task.FromResult(new TestCase
        {
            Title = title,
            Description = description,
            Preconditions = "System is in a valid state",
            TestSteps = GenerateBoundaryTestSteps(requirementText),
            ExpectedResults = "The system should handle boundary conditions correctly",
            Priority = TestCasePriority.High,
            RequirementReference = requirementId,
            CreatedBy = userId
        });
    }

    private string GeneratePositiveTestSteps(string requirementText)
    {
        return $"1. Setup test environment\n2. Input valid data as per requirement\n3. Execute the functionality\n4. Verify the output matches expected results";
    }

    private string GenerateNegativeTestSteps(string requirementText)
    {
        return $"1. Setup test environment\n2. Input invalid/malformed data\n3. Execute the functionality\n4. Verify appropriate error handling";
    }

    private string GenerateBoundaryTestSteps(string requirementText)
    {
        return $"1. Setup test environment\n2. Input boundary values (min/max limits)\n3. Execute the functionality\n4. Verify boundary conditions are handled correctly";
    }

    private bool ContainsBoundaryConditions(string text)
    {
        var boundaryKeywords = new[] { "limit", "maximum", "minimum", "range", "between", "validation" };
        return boundaryKeywords.Any(keyword => text.ToLower().Contains(keyword));
    }

    private string ExtractRelevantContent(string prompt, string frsContent)
    {
        var keywords = ExtractKeywords(prompt);
        var relevantSections = new List<string>();

        foreach (var keyword in keywords)
        {
            var sections = FindSectionsContaining(keyword, frsContent);
            relevantSections.AddRange(sections);
        }

        return string.Join("\n", relevantSections.Take(5));
    }

    private async Task<TestCase> CreateTestCaseFromContent(string prompt, string relevantContent, string userId)
    {
        var title = $"Test Case: {prompt.Take(50)}...";
        var description = $"Generated test case based on user request: {prompt}";
        
        return await Task.FromResult(new TestCase
        {
            Title = title,
            Description = description,
            Preconditions = "System is ready for testing",
            TestSteps = $"1. Follow the scenario: {prompt}\n2. Execute the test\n3. Verify results",
            ExpectedResults = "Results should match the expected behavior described in the FRS",
            Priority = TestCasePriority.Medium,
            RequirementReference = "Custom",
            CreatedBy = userId
        });
    }
}