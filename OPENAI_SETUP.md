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
1. **Check if you're using a placeholder**: Look for values like `your-openai-api-key-here`
2. **Verify the key format**: Should start with `sk-` or `sk-proj-` and be 51-120 characters
3. **Check for typos**: Ensure no extra spaces or characters
4. **Verify the key is active**: Visit https://platform.openai.com/account/api-keys and check status
5. **For project-scoped keys (`sk-proj-`)**:
   - Ensure your project has access to the model you're using (default: `gpt-4o-mini`)
   - Check project permissions and settings
   - Verify project isn't suspended or restricted
6. **Test your key directly**: Use the OpenAI Playground or API directly

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

1. **Starting the application**:
   ```bash
   cd TestCaseAgent.Server
   dotnet run
   ```

2. **Check the startup logs** for configuration validation messages:
   - ✅ `OpenAI API key configuration looks valid: sk-***LgwA`
   - ✅ `Detected project-scoped API key` (for sk-proj- keys)
   - ⚠️ `OpenAI API key appears to be a placeholder value`

3. **Test the AI chat feature** through the web interface

4. **Check application logs** for any API errors

## Troubleshooting Steps

If you're still having issues:

1. **Check the application logs** for specific error messages
2. **Verify your OpenAI account status** at https://platform.openai.com/account
3. **Test your API key directly** using curl:
   ```bash
   curl https://api.openai.com/v1/models \
     -H "Authorization: Bearer sk-your-actual-api-key-here"
   ```
4. **Contact OpenAI support** if the key should be working but isn't

## Configuration Priority

The application checks for the OpenAI API key in this order:

1. `OpenAI:ApiKey` in appsettings.json
2. `OPENAI_API_KEY` environment variable
3. System environment variable `OPENAI_API_KEY`

Later sources override earlier ones, so environment variables will override appsettings.json.