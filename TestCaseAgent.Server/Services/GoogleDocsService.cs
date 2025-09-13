using Google.Apis.Auth.OAuth2;
using Google.Apis.Docs.v1;
using Google.Apis.Docs.v1.Data;
using Google.Apis.Services;
using System.Text;

namespace TestCaseAgent.Server.Services;

public interface IGoogleDocsService
{
    Task<string> GetDocumentContentAsync(string documentId, string accessToken);
    Task<Document> GetDocumentAsync(string documentId, string accessToken);
}

public class GoogleDocsService : IGoogleDocsService
{
    private readonly ILogger<GoogleDocsService> _logger;

    public GoogleDocsService(ILogger<GoogleDocsService> logger)
    {
        _logger = logger;
    }

    public async Task<string> GetDocumentContentAsync(string documentId, string accessToken)
    {
        try
        {
            var credential = GoogleCredential.FromAccessToken(accessToken);
            var service = new DocsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "Test Case Agent"
            });

            var request = service.Documents.Get(documentId);
            var document = await request.ExecuteAsync();

            return ExtractTextFromDocument(document);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving document content for document {DocumentId}", documentId);
            throw;
        }
    }

    public async Task<Document> GetDocumentAsync(string documentId, string accessToken)
    {
        try
        {
            var credential = GoogleCredential.FromAccessToken(accessToken);
            var service = new DocsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "Test Case Agent"
            });

            var request = service.Documents.Get(documentId);
            return await request.ExecuteAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving document for document {DocumentId}", documentId);
            throw;
        }
    }

    private string ExtractTextFromDocument(Document document)
    {
        var text = new StringBuilder();

        if (document.Body?.Content != null)
        {
            foreach (var element in document.Body.Content)
            {
                if (element.Paragraph?.Elements != null)
                {
                    foreach (var paragraphElement in element.Paragraph.Elements)
                    {
                        if (paragraphElement.TextRun?.Content != null)
                        {
                            text.Append(paragraphElement.TextRun.Content);
                        }
                    }
                }
            }
        }

        return text.ToString();
    }
}