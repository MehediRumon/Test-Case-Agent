# Google OAuth 2.0 Setup Guide

## Authorized Redirect URIs Configuration

When configuring Google OAuth 2.0 credentials for the Test Case Agent application, you **must** add the following authorized redirect URIs to your Google Cloud Console project:

### Required Redirect URIs

```
https://localhost:7000/auth/callback
http://localhost:5000/auth/callback
```

### Why These Specific URIs?

1. **`https://localhost:7000/auth/callback`**: 
   - Used when the API server runs in HTTPS mode (production-like environment)
   - Default configuration for secure testing

2. **`http://localhost:5000/auth/callback`**:
   - Used when the API server runs in HTTP mode (development environment)
   - Allows testing without SSL certificates

### Application Port Configuration

The Test Case Agent application is configured with the following ports:

- **API Server (TestCaseAgent.Server)**:
  - HTTPS: `https://localhost:7000`
  - HTTP: `http://localhost:5000`

- **Client Application (TestCaseAgent.Client)**:
  - HTTPS: `https://localhost:5001`
  - HTTP: `http://localhost:5001`

### Setting Up Google OAuth 2.0 Credentials

1. Go to the [Google Cloud Console](https://console.cloud.google.com/)
2. Select your project or create a new one
3. Navigate to **APIs & Services > Credentials**
4. Click **Create Credentials > OAuth 2.0 Client IDs**
5. Configure the OAuth consent screen if not already done
6. Select **Web application** as the application type
7. Add the authorized redirect URIs listed above
8. Save the Client ID and Client Secret
9. Update your `appsettings.json` with these credentials

### Important Notes

- Both HTTP and HTTPS redirect URIs are included to support different development scenarios
- The callback endpoint `/auth/callback` is handled by the API server
- Ensure your Google Cloud project has the following APIs enabled:
  - Google Docs API
  - Google Sheets API
- The OAuth scopes requested by the application are:
  - `https://www.googleapis.com/auth/documents.readonly`
  - `https://www.googleapis.com/auth/spreadsheets`

### Troubleshooting

If you encounter authentication errors:

1. Verify that both redirect URIs are correctly configured in Google Cloud Console
2. Ensure the API server is running on the expected ports (7000 for HTTPS, 5000 for HTTP)
3. Check that your `appsettings.json` contains the correct Client ID and Client Secret
4. Confirm that the required Google APIs are enabled in your project