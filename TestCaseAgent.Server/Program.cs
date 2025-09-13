using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using TestCaseAgent.Server.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
                "https://localhost:5001", 
                "http://localhost:5001",
                "https://localhost:7000",
                "http://localhost:5000",
                "http://localhost:7000")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
    
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Configure Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.LoginPath = "/auth/login";
    options.LogoutPath = "/auth/logout";
})
.AddGoogle(options =>
{
    options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? "";
    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? "";
    options.Scope.Add("https://www.googleapis.com/auth/documents.readonly");
    options.Scope.Add("https://www.googleapis.com/auth/spreadsheets");
});

builder.Services.AddAuthorization();

// Register application services
builder.Services.AddHttpClient<IOpenAIService, OpenAIService>();
builder.Services.AddScoped<IGoogleDocsService, GoogleDocsService>();
builder.Services.AddScoped<IGoogleSheetsService, GoogleSheetsService>();
builder.Services.AddScoped<IIntelligentAgentService, IntelligentAgentService>();
builder.Services.AddSingleton<IDocumentService, DocumentService>(); // Changed to Singleton for in-memory demo storage
builder.Services.AddScoped<IAuditService, AuditService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Only use HTTPS redirection in production or when HTTPS is properly configured
if (!app.Environment.IsDevelopment() || 
    app.Configuration.GetSection("Kestrel:Endpoints:Https").Exists() ||
    !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_HTTPS_PORT")))
{
    app.UseHttpsRedirection();
}

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Authentication endpoints
app.MapGet("/auth/login", () =>
{
    return Results.Challenge(new AuthenticationProperties
    {
        RedirectUri = "/auth/callback"
    }, new List<string> { GoogleDefaults.AuthenticationScheme });
});

app.MapGet("/auth/callback", async (HttpContext context) =>
{
    var result = await context.AuthenticateAsync();
    if (result.Succeeded)
    {
        return Results.Redirect("/");
    }
    return Results.Redirect("/auth/login");
});

app.MapPost("/auth/logout", async (HttpContext context) =>
{
    await context.SignOutAsync();
    return Results.Ok();
});

app.Run();
