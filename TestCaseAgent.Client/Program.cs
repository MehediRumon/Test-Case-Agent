using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using TestCaseAgent.Client;
using TestCaseAgent.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure HttpClient with base address pointing to the API server
// Use HTTP for development to avoid certificate issues
var apiBaseUrl = builder.HostEnvironment.IsDevelopment() 
    ? "http://localhost:5000/" 
    : "https://localhost:7000/";
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });

// Add application services
builder.Services.AddScoped<IApiService, ApiService>();

await builder.Build().RunAsync();
