using System.Net.Http.Json;
using TestCaseAgent.Client.Models;

namespace TestCaseAgent.Client.Services;

public interface IApiService
{
    Task<List<DocumentLink>> GetDocumentsAsync();
    Task<DocumentLink> LinkDocumentAsync(LinkDocumentRequest request);
    Task<bool> UnlinkDocumentAsync(int documentId);
    Task<AgentStatus> GetAgentStatusAsync();
    Task<AgentResponse> AskQuestionAsync(AgentQuery query);
    Task<List<TestCase>> GenerateTestCasesAsync(TestCaseGenerationRequest request);
    Task<TestCase> CreateTestCaseAsync(CreateTestCaseRequest request);
    Task<List<AuditLog>> GetAuditLogsAsync();
    
    // Teacher PIN methods
    Task<Teacher> RegisterTeacherAsync(TeacherRegistrationRequest request);
    Task<TeacherPinResponse> ValidatePinAsync(string userId, TeacherPinRequest request);
    Task<TeacherPinValidationResult> ValidatePinFormatAsync(TeacherPinRequest request);
    Task<Teacher> GetTeacherAsync(string userId);
    Task<bool> ResetPinAsync(string userId, TeacherPinRequest request);
    Task<bool> UnlockAccountAsync(string userId);
    Task<Teacher> CreateSampleTeacherAsync();
}

public class ApiService : IApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ApiService> _logger;

    public ApiService(HttpClient httpClient, ILogger<ApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<DocumentLink>> GetDocumentsAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<List<DocumentLink>>("api/documents");
            return response ?? new List<DocumentLink>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching documents");
            return new List<DocumentLink>();
        }
    }

    public async Task<DocumentLink> LinkDocumentAsync(LinkDocumentRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/documents/link", request);
            response.EnsureSuccessStatusCode();
            
            var result = await response.Content.ReadFromJsonAsync<DocumentLink>();
            return result ?? throw new InvalidOperationException("Failed to parse response");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error linking document");
            throw;
        }
    }

    public async Task<bool> UnlinkDocumentAsync(int documentId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/documents/{documentId}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unlinking document {DocumentId}", documentId);
            return false;
        }
    }

    public async Task<AgentStatus> GetAgentStatusAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<AgentStatus>("api/agent/status");
            return response ?? new AgentStatus();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching agent status");
            return new AgentStatus();
        }
    }

    public async Task<AgentResponse> AskQuestionAsync(AgentQuery query)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/agent/ask", query);
            response.EnsureSuccessStatusCode();
            
            var result = await response.Content.ReadFromJsonAsync<AgentResponse>();
            return result ?? throw new InvalidOperationException("Failed to parse response");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error asking question");
            throw;
        }
    }

    public async Task<List<TestCase>> GenerateTestCasesAsync(TestCaseGenerationRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/agent/generate-testcases", request);
            response.EnsureSuccessStatusCode();
            
            var result = await response.Content.ReadFromJsonAsync<List<TestCase>>();
            return result ?? new List<TestCase>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating test cases");
            throw;
        }
    }

    public async Task<TestCase> CreateTestCaseAsync(CreateTestCaseRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/agent/create-testcase", request);
            response.EnsureSuccessStatusCode();
            
            var result = await response.Content.ReadFromJsonAsync<TestCase>();
            return result ?? throw new InvalidOperationException("Failed to parse response");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating test case");
            throw;
        }
    }

    public async Task<List<AuditLog>> GetAuditLogsAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<List<AuditLog>>("api/audit/my-logs");
            return response ?? new List<AuditLog>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching audit logs");
            return new List<AuditLog>();
        }
    }

    // Teacher PIN implementations
    public async Task<Teacher> RegisterTeacherAsync(TeacherRegistrationRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/teacher/register", request);
            response.EnsureSuccessStatusCode();
            
            var result = await response.Content.ReadFromJsonAsync<Teacher>();
            return result ?? throw new InvalidOperationException("Failed to parse response");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering teacher");
            throw;
        }
    }

    public async Task<TeacherPinResponse> ValidatePinAsync(string userId, TeacherPinRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"api/teacher/validate-pin?userId={userId}", request);
            
            var result = await response.Content.ReadFromJsonAsync<TeacherPinResponse>();
            return result ?? throw new InvalidOperationException("Failed to parse response");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating PIN");
            throw;
        }
    }

    public async Task<TeacherPinValidationResult> ValidatePinFormatAsync(TeacherPinRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/teacher/validate-pin-format", request);
            
            var result = await response.Content.ReadFromJsonAsync<TeacherPinValidationResult>();
            return result ?? throw new InvalidOperationException("Failed to parse response");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating PIN format");
            throw;
        }
    }

    public async Task<Teacher> GetTeacherAsync(string userId)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<Teacher>($"api/teacher/{userId}");
            return response ?? throw new InvalidOperationException("Teacher not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching teacher");
            throw;
        }
    }

    public async Task<bool> ResetPinAsync(string userId, TeacherPinRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"api/teacher/{userId}/reset-pin", request);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting PIN");
            return false;
        }
    }

    public async Task<bool> UnlockAccountAsync(string userId)
    {
        try
        {
            var response = await _httpClient.PostAsync($"api/teacher/{userId}/unlock", null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unlocking account");
            return false;
        }
    }

    public async Task<Teacher> CreateSampleTeacherAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<Teacher>("api/teacher/demo/create-sample");
            return response ?? throw new InvalidOperationException("Failed to create sample teacher");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating sample teacher");
            throw;
        }
    }
}