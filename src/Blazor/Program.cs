using PodcastMetadataGenerator.Blazor.Components;
using PodcastMetadataGenerator.Core.Models;
using PodcastMetadataGenerator.Core.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Register Core services
builder.Services.AddSingleton<AppSettings>(sp =>
{
    var settingsService = new SettingsService();
    return settingsService.Load();
});

builder.Services.AddSingleton<SettingsService>();
builder.Services.AddSingleton<CopilotAuthService>();
builder.Services.AddSingleton<SrtConverter>();
builder.Services.AddSingleton<TranscriptParser>(sp => 
    new TranscriptParser(sp.GetRequiredService<AppSettings>()));
builder.Services.AddSingleton<OutputService>(sp => 
    new OutputService(sp.GetRequiredService<SrtConverter>()));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
