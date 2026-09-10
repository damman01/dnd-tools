using CardStudio.Api.Extensions;
using CardStudio.Api.Modules.Import;
using CardStudio.Api.Modules.Monetization;
using CardStudio.Api.Modules.Rendering;
using CardStudio.Api.Modules.Translation;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container
builder.Services.AddOpenApi();
builder.Services.AddHttpClient();

builder.Services.Configure<UsageLimitOptions>(builder.Configuration.GetSection("Monetization"));
builder.Services.AddSingleton<IQuotaService, UsageLimitService>();
builder.Services.AddSingleton<CardHtmlBuilder>();
builder.Services.AddSingleton<PdfCardService>();
builder.Services.AddSingleton<CardImageService>();
builder.Services.AddSingleton<DndBeyondImportService>();
builder.Services.AddSingleton<ITranslationProvider, D3TranslationProvider>();

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Register modular REST API endpoints
app.MapCardStudioEndpoints();

app.Run();
