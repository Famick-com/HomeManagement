using Famick.HomeManagement.UI.Services;
using Famick.HomeManagement.Web.Client;
using Famick.HomeManagement.Web.Client.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure HttpClient with base address
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});

// Add MudBlazor services
builder.Services.AddMudServices();

// Add authentication services
builder.Services.AddScoped<ITokenStorage, BrowserTokenStorage>();
builder.Services.AddScoped<ApiAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<ApiAuthStateProvider>());
builder.Services.AddScoped<IApiClient, HttpApiClient>();

builder.Services.AddAuthorizationCore();

await builder.Build().RunAsync();
