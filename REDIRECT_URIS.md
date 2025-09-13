# Quick Answer: Authorized Redirect URIs

## What should Authorized redirect URIs be?

For the Test Case Agent application, configure these **exact** redirect URIs in your Google Cloud Console OAuth 2.0 credentials:

```
https://localhost:7000/auth/callback
http://localhost:5000/auth/callback
```

## Why these specific URIs?

- The application server runs on ports 7000 (HTTPS) and 5000 (HTTP)
- The authentication callback endpoint is `/auth/callback`
- Both HTTP and HTTPS are supported for development flexibility

## Where to configure these?

1. Google Cloud Console
2. Navigate to: APIs & Services > Credentials
3. Edit your OAuth 2.0 Client ID
4. Add both URIs to "Authorized redirect URIs"

For detailed setup instructions, see [OAUTH_SETUP.md](OAUTH_SETUP.md)