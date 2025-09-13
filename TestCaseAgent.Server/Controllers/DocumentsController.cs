using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TestCaseAgent.Server.Models;
using TestCaseAgent.Server.Services;

namespace TestCaseAgent.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
// [Authorize] // Commented out for demo purposes to avoid authentication issues
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly IGoogleDocsService _googleDocsService;
    private readonly IGoogleSheetsService _googleSheetsService;
    private readonly IAuditService _auditService;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(
        IDocumentService documentService,
        IGoogleDocsService googleDocsService,
        IGoogleSheetsService googleSheetsService,
        IAuditService auditService,
        ILogger<DocumentsController> logger)
    {
        _documentService = documentService;
        _googleDocsService = googleDocsService;
        _googleSheetsService = googleSheetsService;
        _auditService = auditService;
        _logger = logger;
    }

    [HttpPost("link")]
    public async Task<ActionResult<DocumentLink>> LinkDocument([FromBody] LinkDocumentRequest request)
    {
        try
        {
            var userId = GetUserId();
            var accessToken = GetAccessToken();

            // Validate required fields
            if (string.IsNullOrEmpty(request.DocumentId))
            {
                return BadRequest("Document ID is required");
            }

            if (string.IsNullOrEmpty(request.DocumentUrl))
            {
                return BadRequest("Document URL is required");
            }

            // For demo purposes, skip Google API validation if access token is not available
            // In production, proper OAuth flow should be implemented
            if (string.IsNullOrEmpty(accessToken) || accessToken == "Bearer" || accessToken == "")
            {
                _logger.LogWarning("No valid access token found, skipping Google API validation");
                request.DocumentTitle = request.DocumentTitle ?? $"Document {request.DocumentId}";
            }
            else
            {
                // Validate document access
                try
                {
                    if (request.Type == DocumentType.FunctionalRequirementSpec)
                    {
                        var document = await _googleDocsService.GetDocumentAsync(request.DocumentId, accessToken);
                        request.DocumentTitle = document.Title ?? "Untitled Document";
                    }
                    else if (request.Type == DocumentType.TestCaseSheet)
                    {
                        var spreadsheet = await _googleSheetsService.GetSpreadsheetAsync(request.DocumentId, accessToken);
                        request.DocumentTitle = spreadsheet.Properties?.Title ?? "Untitled Spreadsheet";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to validate document access, proceeding with basic info");
                    request.DocumentTitle = request.DocumentTitle ?? $"Document {request.DocumentId}";
                }
            }

            var documentLink = await _documentService.LinkDocumentAsync(
                userId, 
                request.DocumentId, 
                request.DocumentTitle, 
                request.DocumentUrl, 
                request.Type);

            await _auditService.LogActionAsync(
                userId, 
                "Document Linked", 
                $"Linked {request.Type} document: {request.DocumentTitle}", 
                request.DocumentId);

            return Ok(documentLink);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error linking document");
            return BadRequest($"Failed to link document: {ex.Message}");
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> UnlinkDocument(int id)
    {
        try
        {
            var userId = GetUserId();
            var documentLink = await _documentService.GetDocumentLinkAsync(id);

            if (documentLink == null)
            {
                return NotFound("Document not found");
            }

            if (documentLink.UserId != userId)
            {
                return Forbid("You don't have permission to unlink this document");
            }

            var success = await _documentService.UnlinkDocumentAsync(id, userId);

            if (success)
            {
                await _auditService.LogActionAsync(
                    userId, 
                    "Document Unlinked", 
                    $"Unlinked {documentLink.Type} document: {documentLink.DocumentTitle}", 
                    documentLink.DocumentId);

                return Ok();
            }

            return BadRequest("Failed to unlink document");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unlinking document {DocumentId}", id);
            return BadRequest($"Failed to unlink document: {ex.Message}");
        }
    }

    [HttpGet]
    public async Task<ActionResult<List<DocumentLink>>> GetDocuments()
    {
        try
        {
            var userId = GetUserId();
            var documents = await _documentService.GetUserDocumentsAsync(userId);
            return Ok(documents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving documents for user");
            return BadRequest($"Failed to retrieve documents: {ex.Message}");
        }
    }

    [HttpGet("frs")]
    public async Task<ActionResult<DocumentLink>> GetFRSDocument()
    {
        try
        {
            var userId = GetUserId();
            var frsDocument = await _documentService.GetFRSDocumentAsync(userId);

            if (frsDocument == null)
            {
                return NotFound("No FRS document linked");
            }

            return Ok(frsDocument);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving FRS document for user");
            return BadRequest($"Failed to retrieve FRS document: {ex.Message}");
        }
    }

    [HttpGet("testcases")]
    public async Task<ActionResult<DocumentLink>> GetTestCaseSheet()
    {
        try
        {
            var userId = GetUserId();
            var testCaseSheet = await _documentService.GetTestCaseSheetAsync(userId);

            if (testCaseSheet == null)
            {
                return NotFound("No test case sheet linked");
            }

            return Ok(testCaseSheet);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving test case sheet for user");
            return BadRequest($"Failed to retrieve test case sheet: {ex.Message}");
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
}

public class LinkDocumentRequest
{
    public required string DocumentId { get; set; }
    public string DocumentTitle { get; set; } = "";
    public required string DocumentUrl { get; set; }
    public DocumentType Type { get; set; }
}