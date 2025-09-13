# Test Case Agent

An intelligent test case generation system that integrates with Google Docs (Functional Requirement Specification) and Google Sheets (Test Case management) using the MCP framework.

## Features

- **Document Integration**: Link Google Docs (FRS) and Google Sheets (Test Cases)
- **OpenAI Integration**: Powered by GPT-4o-mini for intelligent responses and test case generation
- **AI Question Answering**: Ask questions about FRS content and get intelligent responses
- **Automated Test Case Generation**: Generate test cases based on requirements using AI
- **Natural Language Interface**: Chat-based interaction for both questions and test case creation
- **Audit Trail**: Complete logging of all actions for auditability
- **Secure Authentication**: Google OAuth integration with proper permission management
- **Traceability**: Maintains links between requirements and generated test cases

## Architecture

- **Backend**: ASP.NET Core 8.0 Web API
- **Frontend**: Blazor WebAssembly
- **Authentication**: Google OAuth 2.0
- **APIs**: Google Docs API, Google Sheets API
- **Framework**: Model-Context-Protocol (MCP) compliant architecture

## Prerequisites

1. **.NET 8.0 SDK** or later
2. **OpenAI API Key** for GPT-4o-mini access
3. **Google Cloud Project** with enabled APIs:
   - Google Docs API
   - Google Sheets API
4. **Google OAuth 2.0 credentials** (Client ID and Client Secret)

## Setup Instructions

### 1. Google Cloud Configuration

1. Go to the [Google Cloud Console](https://console.cloud.google.com/)
2. Create a new project or select an existing one
3. Enable the following APIs:
   - Google Docs API
   - Google Sheets API
4. Create OAuth 2.0 credentials:
   - Go to APIs & Services > Credentials
   - Create OAuth 2.0 Client IDs
   - Add authorized redirect URIs:
     - `https://localhost:7000/auth/callback`
     - `http://localhost:5000/auth/callback`
5. Note down the Client ID and Client Secret

### 2. Application Configuration

1. Clone the repository
2. Update `TestCaseAgent.Server/appsettings.json`:
   ```json
   {
     "Authentication": {
       "Google": {
         "ClientId": "your-google-client-id",
         "ClientSecret": "your-google-client-secret"
       }
     },
     "OpenAI": {
       "ApiKey": "your-openai-api-key",
       "Model": "gpt-4o-mini",
       "MaxTokens": 2000,
       "Temperature": 0.7
     }
   }
   ```

### 3. Running the Application

1. Start the API server:
   ```bash
   cd TestCaseAgent.Server
   dotnet run
   ```
   The API will be available at `https://localhost:7000`

2. Start the Blazor client:
   ```bash
   cd TestCaseAgent.Client
   dotnet run
   ```
   The web application will be available at `https://localhost:5001`

## Usage

### 1. Document Setup
1. Navigate to the **Documents** page
2. Link your Google Docs containing the FRS
3. Link your Google Sheets for test case management

### 2. AI Chat
1. Go to the **AI Chat** page
2. Ask questions about your FRS document
3. Get intelligent responses with references to source material

### 3. Test Case Generation
1. Visit the **Test Cases** page
2. Generate test cases from specific requirements
3. Create custom test cases using natural language prompts
4. View generated test cases and their details

### 4. Audit Trail
1. Check the **Audit Logs** page
2. Review all actions performed by users and the AI agent
3. Track document linkage and test case generation activities

## Project Structure

```
TestCaseAgent/
├── TestCaseAgent.Server/          # ASP.NET Core Web API
│   ├── Controllers/               # API Controllers
│   ├── Models/                    # Data Models
│   ├── Services/                  # Business Logic Services
│   └── Program.cs                 # Application Entry Point
├── TestCaseAgent.Client/          # Blazor WebAssembly
│   ├── Pages/                     # Razor Pages/Components
│   ├── Models/                    # Client-side Models
│   ├── Services/                  # API Communication Services
│   └── Program.cs                 # Client Entry Point
└── README.md                      # This file
```

## Key Components

### Backend Services
- **OpenAIService**: Integrates with OpenAI API for intelligent responses and test case generation
- **GoogleDocsService**: Integrates with Google Docs API to read FRS content
- **GoogleSheetsService**: Manages test case data in Google Sheets
- **IntelligentAgentService**: Orchestrates AI-powered features with fallback to basic processing
- **DocumentService**: Manages document links and metadata
- **AuditService**: Tracks all user and system actions

### Frontend Pages
- **Dashboard**: Status overview and quick actions
- **Documents**: Document linking and management
- **AI Chat**: Interactive question-answering interface
- **Test Cases**: Test case generation and viewing
- **Audit Logs**: Activity tracking and logging

## Security Features

- Google OAuth 2.0 authentication
- Secure API communication
- User-specific document access
- Comprehensive audit logging
- CORS protection

## Development

### Adding New Features
1. Backend: Add services in `Services/` and controllers in `Controllers/`
2. Frontend: Create components in `Pages/` and update navigation
3. Models: Update both client and server model classes as needed

### Testing
```bash
# Build the entire solution
dotnet build

# Run the server
cd TestCaseAgent.Server && dotnet run

# Run the client
cd TestCaseAgent.Client && dotnet run
```

## License

This project is provided as-is for educational and demonstration purposes.

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Submit a pull request

## Support

For issues and questions, please create an issue in the repository.