# OpenAI API Configuration Guide

This guide will help you properly configure the OpenAI API key for the Test Case Agent application.

## Prerequisites

1. **OpenAI Account**: You need an active OpenAI account
2. **API Key**: You need a valid OpenAI API key with sufficient credits
3. **Valid Format**: OpenAI API keys can have different formats:
   - **Traditional keys**: Start with `sk-` (e.g., `sk-1234567890abcdef...`)
   - **Project-scoped keys**: Start with `sk-proj-` (e.g., `sk-proj-1234567890abcdef...`)
   - Both types are typically 51-120 characters long

## Getting Your OpenAI API Key

1. Go to [OpenAI Platform](https://platform.openai.com/account/api-keys)
2. Sign in to your OpenAI account
3. Click "Create new secret key"
4. Copy the key immediately (you won't be able to see it again)
5. Store it securely

## Configuration Methods

### Method 1: Using appsettings.json (Recommended for production)

Edit `TestCaseAgent.Server/appsettings.json`:

```json
{
  "OpenAI": {
    "ApiKey": "sk-your-actual-openai-api-key-here",
    "Model": "gpt-4o-mini",
    "MaxTokens": 2000,
    "Temperature": 0.7
  }
}
```

### Method 2: Using Environment Variables (Recommended for deployment)

Set the environment variable before running the application:

**Windows:**
```cmd
set OPENAI_API_KEY=sk-your-actual-openai-api-key-here
dotnet run
```

**Linux/Mac:**
```bash
export OPENAI_API_KEY=sk-your-actual-openai-api-key-here
dotnet run
```

### Method 3: Using .NET User Secrets (Recommended for development)

This is the most secure method for development:

```bash
cd TestCaseAgent.Server
dotnet user-secrets set "OpenAI:ApiKey" "sk-your-actual-openai-api-key-here"
```

## Understanding OpenAI API Key Types

### Traditional API Keys (`sk-`)
- Format: `sk-1234567890abcdef...`
- Account-level access
- Access to all models available to your account
- Simpler permission model

### Project-Scoped API Keys (`sk-proj-`)
- Format: `sk-proj-1234567890abcdef...`
- Limited to specific OpenAI projects
- More granular access control
- Recommended for production applications

**Important for Project-Scoped Keys:**
- Ensure your project has access to the model specified in configuration (`gpt-4o-mini` by default)
- Check project status and permissions at https://platform.openai.com/settings/organization/projects
- Verify project billing and usage limits

## Configuration Validation

The application now includes automatic configuration validation that will:

1. **Check at startup** if the API key is configured
2. **Detect placeholder values** and warn you if you're using example keys
3. **Validate the format** to ensure it looks like a valid OpenAI API key
4. **Provide helpful error messages** with configuration instructions

## Common Issues and Solutions

### Issue: "Incorrect API key provided" Error

**Symptoms:**
```
OpenAI API error: Unauthorized - {
    "error": {
        "message": "Incorrect API key provided: sk-proj-************************************************************LgwA",
        "type": "invalid_request_error",
        "param": null,
        "code": "invalid_api_key"
    }
}
```

**Solutions:**

#### Step 1: Verify API Key Format and Configuration
1. **Check if you're using a placeholder**: Look for values like `your-openai-api-key-here`
2. **Verify the key format**: Should start with `sk-` or `sk-proj-` and be 51-120 characters
3. **Check for typos**: Ensure no extra spaces or characters
4. **Test configuration loading**: Use the diagnostics endpoint:
   ```bash
   curl http://localhost:5000/api/diagnostics/configuration-info
   ```

#### Step 2: Validate API Key with OpenAI
1. **Check API key status**: Visit https://platform.openai.com/account/api-keys and verify:
   - Key shows as "Active" (green indicator)
   - Key hasn't been revoked or expired
   - Last used date is recent if you've used it elsewhere

2. **Test the key directly** using the diagnostics endpoint:
   ```bash
   curl http://localhost:5000/api/diagnostics/openai-status
   ```

3. **Test with curl directly**:
   ```bash
   curl -H "Authorization: Bearer sk-your-actual-api-key" https://api.openai.com/v1/models
   ```

#### Step 3: For Project-Scoped Keys (`sk-proj-*`)
Project-scoped keys have additional requirements:

1. **Check project status**:
   - Visit: https://platform.openai.com/settings/organization/projects
   - Ensure your project exists and is active
   - Verify project isn't suspended or restricted

2. **Verify model access**:
   - Ensure the project has access to model: `gpt-4o-mini` (default)
   - Check project permissions and quotas
   - Some models may not be available to all projects

3. **Check project billing**:
   - Ensure project has billing configured
   - Verify sufficient credits in project budget
   - Check project-specific usage limits

#### Step 4: Account and Billing Issues
1. **Verify billing status**: Visit https://platform.openai.com/account/billing
2. **Check account standing**: Ensure account is in good standing
3. **Review usage limits**: Check you haven't exceeded monthly quotas
4. **Add payment method**: If using free tier, consider upgrading

#### Step 5: Configuration Troubleshooting
1. **Configuration precedence**: Environment variables override appsettings.json
2. **Restart application**: Configuration may be cached
3. **Check file location**: Ensure appsettings.json is in the correct directory
4. **Validate JSON syntax**: Ensure valid JSON format

#### Step 6: Advanced Diagnostics
The application now includes enhanced diagnostics:

1. **Real-time validation**: The app tests your API key on startup
2. **Detailed error messages**: Specific guidance for different error types
3. **Configuration detection**: Shows which configuration source is being used
4. **Key type identification**: Distinguishes between traditional and project-scoped keys

### Issue: Configuration Not Loading

**Symptoms:**
- Application warns about missing API key at startup
- Falls back to basic processing

**Solutions:**
1. **Check configuration precedence**: Environment variables override appsettings.json
2. **Verify file location**: Ensure appsettings.json is in the correct directory
3. **Check JSON syntax**: Ensure valid JSON format
4. **Use user secrets for development**: Keeps keys out of source control

### Issue: API Key Works But Gets Rate Limited

**Symptoms:**
```
OpenAI API error: TooManyRequests
```

**Solutions:**
1. **Check your usage limits**: Visit https://platform.openai.com/account/usage
2. **Add billing information**: Ensure you have sufficient credits
3. **Upgrade your plan**: Free tier has limited requests

## Security Best Practices

1. **Never commit API keys to source control**
2. **Use user secrets for development**
3. **Use environment variables for production deployment**
4. **Rotate keys regularly**
5. **Monitor usage and billing**

## Testing Your Configuration

After configuring your API key, you can test it by:

### Method 1: Application Startup Validation
1. **Start the application**:
   ```bash
   cd TestCaseAgent.Server
   dotnet run
   ```

2. **Check the startup logs** for configuration validation messages:
   - ✅ `OpenAI API key validated successfully: sk-***LgwA`
   - ✅ `Detected project-scoped API key` (for sk-proj- keys)
   - ⚠️ `OpenAI API key appears to be a placeholder value`
   - ❌ `OpenAI API key validation failed` (with specific error details)

### Method 2: Diagnostics API Endpoints
The application provides real-time diagnostics:

1. **Configuration Information**:
   ```bash
   curl http://localhost:5000/api/diagnostics/configuration-info
   ```
   Returns:
   ```json
   {
     "openAI": {
       "hasApiKey": true,
       "keyType": "Project-scoped",
       "model": "gpt-4o-mini",
       "configurationSource": "appsettings.json"
     }
   }
   ```

2. **API Key Validation**:
   ```bash
   curl http://localhost:5000/api/diagnostics/openai-status
   ```
   Returns detailed validation results:
   ```json
   {
     "isValid": false,
     "errorMessage": "OpenAI API returned Unauthorized: {...}",
     "recommendation": "For project-scoped keys: verify project has necessary permissions",
     "keyType": "Project-scoped"
   }
   ```

3. **Test API Call**:
   ```bash
   curl -X POST -H "Content-Type: application/json" \
        -d '"Hello, this is a test."' \
        http://localhost:5000/api/diagnostics/test-openai
   ```

### Method 3: Web Interface Testing
1. Navigate to the web application at `https://localhost:5001`
2. Go to the **AI Chat** page
3. Ask a test question to verify the AI features work
4. Check application logs for any API errors

### Method 4: Direct API Testing
Test your API key outside the application:
```bash
curl -H "Authorization: Bearer sk-your-actual-api-key" \
     https://api.openai.com/v1/models
```

## "My API Key is Correct But Still Getting Errors"

If you're confident your API key is correct but still experiencing authentication failures, follow this systematic troubleshooting approach:

### Quick Diagnostics
1. **Run the built-in diagnostics**:
   ```bash
   curl http://localhost:5000/api/diagnostics/openai-status
   ```
   This will show exactly what error OpenAI is returning and provide specific recommendations.

2. **Check the exact error message**. Common scenarios:

#### Scenario 1: "Incorrect API key provided"
- **Cause**: Key is invalid, revoked, or has insufficient permissions
- **Solution**: 
  - Verify key is active at https://platform.openai.com/account/api-keys
  - For project keys: check project permissions and model access
  - Try regenerating the key if it might be compromised

#### Scenario 2: Network/DNS Issues
- **Error**: `Name or service not known (api.openai.com:443)`
- **Cause**: Network connectivity or DNS resolution problems
- **Solution**: 
  - Check internet connectivity
  - Try from a different network
  - Verify firewall/proxy settings

#### Scenario 3: Rate Limiting or Quota Exceeded
- **Error**: `TooManyRequests` or billing-related errors
- **Cause**: Account limits exceeded
- **Solution**: 
  - Check usage at https://platform.openai.com/account/usage
  - Add billing information or upgrade plan
  - Wait for quota reset

#### Scenario 4: Model Access Issues (Project-Scoped Keys)
- **Error**: Model not available or access denied
- **Cause**: Project doesn't have access to the requested model
- **Solution**:
  - Check project settings at https://platform.openai.com/settings/organization/projects
  - Ensure project has access to `gpt-4o-mini`
  - Verify project billing and limits

### Step-by-Step Verification Process

1. **Format Check**: 
   ```bash
   # Your key should look like one of these:
   sk-1234567890abcdef1234567890abcdef1234567890abcdef123  # Traditional
   sk-proj-1234567890abcdef1234567890abcdef1234567890abcdef123  # Project-scoped
   ```

2. **Direct API Test**:
   ```bash
   curl -H "Authorization: Bearer YOUR_KEY_HERE" \
        https://api.openai.com/v1/models
   ```
   If this fails, the issue is with the key itself, not the application.

3. **Configuration Verification**:
   ```bash
   curl http://localhost:5000/api/diagnostics/configuration-info
   ```
   Ensure the application is loading your key from the expected source.

4. **Test with Known-Good Key**:
   - If possible, test with a different API key
   - Or test your key in OpenAI Playground: https://platform.openai.com/playground

5. **Check Account Status**:
   - Visit https://platform.openai.com/account/billing
   - Ensure account is in good standing
   - Verify payment method and billing information

### Still Having Issues?

If none of the above resolves the issue:

1. **Enable detailed logging** by setting log level to Debug in appsettings.json
2. **Contact OpenAI support** with your account details (never share your actual API key)
3. **Try creating a new API key** - sometimes keys can become corrupted
4. **Check OpenAI status** at https://status.openai.com for service issues

The enhanced error handling in this application will provide specific, actionable guidance for most common scenarios.

## Configuration Priority

The application checks for the OpenAI API key in this order:

1. `OpenAI:ApiKey` in appsettings.json
2. `OPENAI_API_KEY` environment variable
3. System environment variable `OPENAI_API_KEY`

Later sources override earlier ones, so environment variables will override appsettings.json.