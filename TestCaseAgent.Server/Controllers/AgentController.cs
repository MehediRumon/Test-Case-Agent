using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TestCaseAgent.Server.Models;
using TestCaseAgent.Server.Services;

namespace TestCaseAgent.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
// [Authorize] // Commented out for demo purposes to avoid authentication issues
public class AgentController : ControllerBase
{
    private readonly IIntelligentAgentService _agentService;
    private readonly IDocumentService _documentService;
    private readonly IGoogleDocsService _googleDocsService;
    private readonly IGoogleSheetsService _googleSheetsService;
    private readonly IAuditService _auditService;
    private readonly ILogger<AgentController> _logger;

    public AgentController(
        IIntelligentAgentService agentService,
        IDocumentService documentService,
        IGoogleDocsService googleDocsService,
        IGoogleSheetsService googleSheetsService,
        IAuditService auditService,
        ILogger<AgentController> logger)
    {
        _agentService = agentService;
        _documentService = documentService;
        _googleDocsService = googleDocsService;
        _googleSheetsService = googleSheetsService;
        _auditService = auditService;
        _logger = logger;
    }

    [HttpPost("ask")]
    public async Task<ActionResult<AgentResponse>> AskQuestion([FromBody] AgentQuery query)
    {
        try
        {
            var userId = GetUserId();
            var accessToken = GetAccessToken();

            // Get linked FRS document
            var frsDocument = await _documentService.GetFRSDocumentAsync(userId);
            if (frsDocument == null)
            {
                return BadRequest("No FRS document is linked. Please link a Google Doc containing the Functional Requirements Specification first.");
            }

            // For demo purposes, skip Google API access if access token is not available
            string frsContent;
            if (string.IsNullOrEmpty(accessToken) || accessToken == "Bearer" || accessToken == "")
            {
                _logger.LogInformation("Running in demo mode without Google API access token, using mock FRS content");
                frsContent = GetMockFRSContent(frsDocument.DocumentId);
            }
            else
            {
                // Retrieve FRS content from Google Docs
                frsContent = await _googleDocsService.GetDocumentContentAsync(frsDocument.DocumentId, accessToken);
            }

            // Process the question
            var response = await _agentService.AnswerQuestionAsync(query.Question, frsContent);

            // Log the interaction
            await _auditService.LogActionAsync(
                userId,
                "Question Asked",
                $"Question: {query.Question}",
                frsDocument.DocumentId);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing question: {Question}", query.Question);
            return BadRequest($"Failed to process question: {ex.Message}");
        }
    }

    [HttpPost("generate-testcases")]
    public async Task<ActionResult<List<TestCase>>> GenerateTestCases([FromBody] TestCaseGenerationRequest request)
    {
        try
        {
            var userId = GetUserId();
            var accessToken = GetAccessToken();

            // Generate test cases
            var testCases = await _agentService.GenerateTestCasesAsync(
                request.RequirementText,
                request.RequirementId,
                userId);

            // Get linked test case sheet
            var testCaseSheet = await _documentService.GetTestCaseSheetAsync(userId);
            if (testCaseSheet != null)
            {
                // For demo purposes, skip Google Sheets API access if access token is not available
                if (!string.IsNullOrEmpty(accessToken) && accessToken != "Bearer" && accessToken != "")
                {
                    // Add test cases to Google Sheets
                    foreach (var testCase in testCases)
                    {
                        await _googleSheetsService.AppendTestCaseAsync(testCaseSheet.DocumentId, testCase, accessToken);
                    }
                }
                else
                {
                    _logger.LogInformation("Running in demo mode without Google API access token, skipping sheet append");
                }

                await _auditService.LogActionAsync(
                    userId,
                    "Test Cases Generated",
                    $"Generated {testCases.Count} test cases for requirement: {request.RequirementId}",
                    testCaseSheet.DocumentId);
            }
            else
            {
                await _auditService.LogActionAsync(
                    userId,
                    "Test Cases Generated",
                    $"Generated {testCases.Count} test cases for requirement: {request.RequirementId} (No sheet linked)");
            }

            return Ok(testCases);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating test cases for requirement: {RequirementId}", request.RequirementId);
            return BadRequest($"Failed to generate test cases: {ex.Message}");
        }
    }

    [HttpPost("create-testcase")]
    public async Task<ActionResult<TestCase>> CreateTestCase([FromBody] CreateTestCaseRequest request)
    {
        try
        {
            var userId = GetUserId();
            var accessToken = GetAccessToken();

            // Get linked FRS document
            var frsDocument = await _documentService.GetFRSDocumentAsync(userId);
            string frsContent = "";

            if (frsDocument != null)
            {
                // For demo purposes, skip Google API access if access token is not available
                if (string.IsNullOrEmpty(accessToken) || accessToken == "Bearer" || accessToken == "")
                {
                    _logger.LogInformation("Running in demo mode without Google API access token, using mock FRS content");
                    frsContent = GetMockFRSContent(frsDocument.DocumentId);
                }
                else
                {
                    frsContent = await _googleDocsService.GetDocumentContentAsync(frsDocument.DocumentId, accessToken);
                }
            }

            // Generate test case from prompt
            var testCase = await _agentService.GenerateTestCaseFromPromptAsync(request.Prompt, frsContent, userId);

            // Get linked test case sheet and add the test case
            var testCaseSheet = await _documentService.GetTestCaseSheetAsync(userId);
            if (testCaseSheet != null)
            {
                // For demo purposes, skip Google Sheets API access if access token is not available
                if (!string.IsNullOrEmpty(accessToken) && accessToken != "Bearer" && accessToken != "")
                {
                    await _googleSheetsService.AppendTestCaseAsync(testCaseSheet.DocumentId, testCase, accessToken);
                }
                else
                {
                    _logger.LogInformation("Running in demo mode without Google API access token, skipping sheet append");
                }

                await _auditService.LogActionAsync(
                    userId,
                    "Custom Test Case Created",
                    $"Created test case from prompt: {request.Prompt}",
                    testCaseSheet.DocumentId);
            }
            else
            {
                await _auditService.LogActionAsync(
                    userId,
                    "Custom Test Case Created",
                    $"Created test case from prompt: {request.Prompt} (No sheet linked)");
            }

            return Ok(testCase);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating test case from prompt: {Prompt}", request.Prompt);
            return BadRequest($"Failed to create test case: {ex.Message}");
        }
    }

    [HttpGet("status")]
    public async Task<ActionResult<AgentStatus>> GetAgentStatus()
    {
        try
        {
            var userId = GetUserId();
            var frsDocument = await _documentService.GetFRSDocumentAsync(userId);
            var testCaseSheet = await _documentService.GetTestCaseSheetAsync(userId);

            var status = new AgentStatus
            {
                HasFRSDocument = frsDocument != null,
                HasTestCaseSheet = testCaseSheet != null,
                FRSDocumentTitle = frsDocument?.DocumentTitle,
                TestCaseSheetTitle = testCaseSheet?.DocumentTitle,
                IsReady = frsDocument != null && testCaseSheet != null
            };

            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting agent status");
            return BadRequest($"Failed to get agent status: {ex.Message}");
        }
    }

    private string GetUserId()
    {
        var nameIdentifier = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(nameIdentifier))
        {
            return nameIdentifier;
        }

        // Fallback: try to get from other claims
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        if (!string.IsNullOrEmpty(email))
        {
            return email;
        }

        var name = User.FindFirst(ClaimTypes.Name)?.Value;
        if (!string.IsNullOrEmpty(name))
        {
            return name;
        }

        // Demo fallback
        return "demo-user";
    }

    private string GetAccessToken()
    {
        return HttpContext.Request.Headers["Authorization"]
            .FirstOrDefault()?.Split(" ").Last() ?? "";
    }

    private string GetMockFRSContent(string documentId)
    {
        // Return mock FRS content for demo purposes when Google API access is not available
        // This helps demonstrate the application functionality without requiring actual Google Doc access
        return $@"FUNCTIONAL REQUIREMENTS SPECIFICATION - Document ID: {documentId}

1. Teacher PIN Field Requirements:

1.1 Purpose: The Teacher PIN field allows teachers to securely access the system and verify their identity.

1.2 Functionality:
- Teachers must enter a 6-digit numeric PIN
- The PIN must be validated against the teacher database
- The system must provide appropriate feedback for valid/invalid PINs
- PIN entry attempts must be logged for security purposes

1.3 Input Validation:
- Only numeric characters (0-9) are allowed
- Exactly 6 digits must be entered
- Leading zeros are permitted
- The field must be masked for security (display asterisks or dots)

1.4 Security Requirements:
- PIN must be encrypted when stored
- Maximum 3 failed attempts allowed before account lockout
- Account lockout duration: 15 minutes
- PIN must expire after 90 days and require reset

1.5 User Interface Requirements:
- Clear labeling: 'Teacher PIN'
- Input field with masked characters
- Submit button to verify PIN
- Error messages for invalid inputs
- Success message for valid PIN entry

1.6 System Behavior:
- Upon successful PIN entry, redirect to teacher dashboard
- Failed attempts should display appropriate error messages
- System must maintain session security after successful authentication

This mock content is provided for demonstration purposes when Google Docs API access is not available.";
    }
}

public class CreateTestCaseRequest
{
    public required string Prompt { get; set; }
}

public class AgentStatus
{
    public bool HasFRSDocument { get; set; }
    public bool HasTestCaseSheet { get; set; }
    public string? FRSDocumentTitle { get; set; }
    public string? TestCaseSheetTitle { get; set; }
    public bool IsReady { get; set; }
}