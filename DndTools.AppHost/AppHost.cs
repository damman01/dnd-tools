var builder = DistributedApplication.CreateBuilder(args);

// AddParameter(name, value) always uses "value" as a hardcoded literal - it does NOT fall
// back to configuration. To keep these overridable (AppHost appsettings.json, user secrets,
// or Parameters__* env vars) while still having a sensible default, resolve the override
// ourselves first and only use the hardcoded default when nothing was configured.
var freeCardLimitPerDeck = builder.AddParameter(
    "monetization-free-card-limit",
    builder.Configuration["Parameters:monetization-free-card-limit"] ?? "8",
    publishValueAsDefault: true);
var freeGenerationsPerDay = builder.AddParameter(
    "monetization-free-generations-per-day",
    builder.Configuration["Parameters:monetization-free-generations-per-day"] ?? "5",
    publishValueAsDefault: true);

builder.AddProject<Projects.DndCards_Web>("web")
    .WithEnvironment("Monetization__FreeCardLimitPerDeck", freeCardLimitPerDeck)
    .WithEnvironment("Monetization__FreeGenerationsPerDay", freeGenerationsPerDay);

builder.Build().Run();
