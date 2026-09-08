var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.DndCards_Web>("web");

builder.Build().Run();
