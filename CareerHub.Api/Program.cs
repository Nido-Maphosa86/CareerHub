using Scalar.AspNetCore;

// Phase 1: Builder - Register the services into the app
// Dependency injection container

var builder = WebApplication.CreateBuilder(args);

// Register your services

builder.Services.AddControllers();   // registering controller support
builder.Services.AddOpenApi();       // registering built-in OpenApi document generation

var app = builder.Build();           // Nothing can be registered after this

// Phase 2: Pipeline - Configure your middleware chain
// NB: Order matters!!

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapControllers();

app.Run();
