using TestCaseAgent.Server.Models;

namespace TestCaseAgent.Server.Services;

public interface IDocumentService
{
    Task<DocumentLink> LinkDocumentAsync(string userId, string documentId, string documentTitle, string documentUrl, DocumentType type);
    Task<bool> UnlinkDocumentAsync(int documentLinkId, string userId);
    Task<List<DocumentLink>> GetUserDocumentsAsync(string userId);
    Task<DocumentLink?> GetDocumentLinkAsync(int id);
    Task<DocumentLink?> GetFRSDocumentAsync(string userId);
    Task<DocumentLink?> GetTestCaseSheetAsync(string userId);
}

public class DocumentService : IDocumentService
{
    private readonly ILogger<DocumentService> _logger;
    private readonly List<DocumentLink> _documentLinks; // In-memory storage for demo

    public DocumentService(ILogger<DocumentService> logger)
    {
        _logger = logger;
        _documentLinks = new List<DocumentLink>();
    }

    public async Task<DocumentLink> LinkDocumentAsync(string userId, string documentId, string documentTitle, string documentUrl, DocumentType type)
    {
        try
        {
            // Deactivate existing document of the same type for this user
            var existing = _documentLinks
                .Where(d => d.UserId == userId && d.Type == type && d.IsActive)
                .ToList();

            foreach (var doc in existing)
            {
                doc.IsActive = false;
            }

            var documentLink = new DocumentLink
            {
                Id = _documentLinks.Count + 1,
                UserId = userId,
                DocumentId = documentId,
                DocumentTitle = documentTitle,
                DocumentUrl = documentUrl,
                Type = type
            };

            _documentLinks.Add(documentLink);

            _logger.LogInformation("Document linked: {DocumentId} for user {UserId}", documentId, userId);

            return await Task.FromResult(documentLink);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error linking document {DocumentId} for user {UserId}", documentId, userId);
            throw;
        }
    }

    public async Task<bool> UnlinkDocumentAsync(int documentLinkId, string userId)
    {
        try
        {
            var documentLink = _documentLinks
                .FirstOrDefault(d => d.Id == documentLinkId && d.UserId == userId);

            if (documentLink != null)
            {
                documentLink.IsActive = false;
                _logger.LogInformation("Document unlinked: {DocumentLinkId} for user {UserId}", documentLinkId, userId);
                return await Task.FromResult(true);
            }

            return await Task.FromResult(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unlinking document {DocumentLinkId} for user {UserId}", documentLinkId, userId);
            throw;
        }
    }

    public async Task<List<DocumentLink>> GetUserDocumentsAsync(string userId)
    {
        try
        {
            var documents = _documentLinks
                .Where(d => d.UserId == userId && d.IsActive)
                .OrderByDescending(d => d.CreatedAt)
                .ToList();

            return await Task.FromResult(documents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving documents for user {UserId}", userId);
            throw;
        }
    }

    public async Task<DocumentLink?> GetDocumentLinkAsync(int id)
    {
        try
        {
            var document = _documentLinks
                .FirstOrDefault(d => d.Id == id && d.IsActive);

            return await Task.FromResult(document);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving document link {Id}", id);
            throw;
        }
    }

    public async Task<DocumentLink?> GetFRSDocumentAsync(string userId)
    {
        try
        {
            var frsDocument = _documentLinks
                .FirstOrDefault(d => d.UserId == userId && d.Type == DocumentType.FunctionalRequirementSpec && d.IsActive);

            return await Task.FromResult(frsDocument);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving FRS document for user {UserId}", userId);
            throw;
        }
    }

    public async Task<DocumentLink?> GetTestCaseSheetAsync(string userId)
    {
        try
        {
            var testCaseSheet = _documentLinks
                .FirstOrDefault(d => d.UserId == userId && d.Type == DocumentType.TestCaseSheet && d.IsActive);

            return await Task.FromResult(testCaseSheet);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving test case sheet for user {UserId}", userId);
            throw;
        }
    }
}