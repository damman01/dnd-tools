using CardStudio.Web.Components;
using CardStudio.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpClient<ICardApiClient, CardApiClient>(client =>
{
    client.BaseAddress = new Uri("https+http://api");
});

builder.Services.Configure<UsageLimitOptions>(builder.Configuration.GetSection("Monetization"));
builder.Services.AddSingleton<UsageLimitService>();
builder.Services.AddSingleton<CardHtmlBuilder>();
builder.Services.AddSingleton<DndBeyondImportService>();
builder.Services.AddSingleton<CardTranslationService>();
builder.Services.AddScoped<LocalizationService>();
builder.Services.AddScoped<CardDeckStateService>();

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
