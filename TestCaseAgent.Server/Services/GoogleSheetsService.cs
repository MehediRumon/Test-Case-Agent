using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using TestCaseAgent.Server.Models;

namespace TestCaseAgent.Server.Services;

public interface IGoogleSheetsService
{
    Task<IList<IList<object>>> GetSheetDataAsync(string spreadsheetId, string range, string accessToken);
    Task AppendTestCaseAsync(string spreadsheetId, TestCase testCase, string accessToken);
    Task<Spreadsheet> GetSpreadsheetAsync(string spreadsheetId, string accessToken);
}

public class GoogleSheetsService : IGoogleSheetsService
{
    private readonly ILogger<GoogleSheetsService> _logger;

    public GoogleSheetsService(ILogger<GoogleSheetsService> logger)
    {
        _logger = logger;
    }

    public async Task<IList<IList<object>>> GetSheetDataAsync(string spreadsheetId, string range, string accessToken)
    {
        try
        {
            var credential = GoogleCredential.FromAccessToken(accessToken);
            var service = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "Test Case Agent"
            });

            var request = service.Spreadsheets.Values.Get(spreadsheetId, range);
            var response = await request.ExecuteAsync();

            return response.Values ?? new List<IList<object>>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving sheet data for spreadsheet {SpreadsheetId}", spreadsheetId);
            throw;
        }
    }

    public async Task AppendTestCaseAsync(string spreadsheetId, TestCase testCase, string accessToken)
    {
        try
        {
            var credential = GoogleCredential.FromAccessToken(accessToken);
            var service = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "Test Case Agent"
            });

            var values = new List<IList<object>>
            {
                new List<object>
                {
                    testCase.Title,
                    testCase.Description,
                    testCase.Preconditions,
                    testCase.TestSteps,
                    testCase.ExpectedResults,
                    testCase.Priority.ToString(),
                    testCase.RequirementReference,
                    testCase.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                    testCase.CreatedBy
                }
            };

            var valueRange = new ValueRange
            {
                Values = values
            };

            var request = service.Spreadsheets.Values.Append(valueRange, spreadsheetId, "A:I");
            request.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
            request.InsertDataOption = SpreadsheetsResource.ValuesResource.AppendRequest.InsertDataOptionEnum.INSERTROWS;

            await request.ExecuteAsync();

            _logger.LogInformation("Test case appended to spreadsheet {SpreadsheetId}", spreadsheetId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error appending test case to spreadsheet {SpreadsheetId}", spreadsheetId);
            throw;
        }
    }

    public async Task<Spreadsheet> GetSpreadsheetAsync(string spreadsheetId, string accessToken)
    {
        try
        {
            var credential = GoogleCredential.FromAccessToken(accessToken);
            var service = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "Test Case Agent"
            });

            var request = service.Spreadsheets.Get(spreadsheetId);
            return await request.ExecuteAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving spreadsheet {SpreadsheetId}", spreadsheetId);
            throw;
        }
    }
}