using Microsoft.AspNetCore.Mvc;
using TinyUrl.Api.Domain;
using TinyUrl.Api.Infrastructure.Repositories;
using TinyUrl.Api.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Minimal CORS for local dev UI.
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy
            .AllowAnyHeader()
            .AllowAnyMethod()
            .WithOrigins(
                "http://localhost:5173", // Vite default
                "http://localhost:3000"  // alt
            ));
});

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    // We keep default behavior; explicit checks in controllers.
});

// Dependency Injection (Domain -> Infrastructure)
builder.Services.AddSingleton<IUrlRepository, InMemoryUrlRepository>();
builder.Services.AddSingleton<ICodeGenerator, Base62CodeGenerator>();
builder.Services.AddSingleton<IUrlService, UrlService>();
builder.Services.AddSingleton<IUserContext, HeaderUserContext>();

var app = builder.Build();

app.UseCors();

// Basic error handling (keep responses predictable for a POC).
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { error = "Unexpected error", detail = ex.Message });
    }
});

app.MapControllers();

app.Run();

