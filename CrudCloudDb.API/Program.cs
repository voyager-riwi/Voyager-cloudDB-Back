// =======================
// 1️⃣ Using Statements
// =======================

using CrudCloudDb.Infrastructure.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// =======================
//  Builder Initialization
// =======================
var builder = WebApplication.CreateBuilder(args);
// Configurar para escuchar en puerto 8081 (Docker)
builder.WebHost.UseUrls("http://0.0.0.0:8081");

// Configurar CORS para que el frontend pueda consumir
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString)
        .UseSnakeCaseNamingConvention());



// =======================
//  Service Configuration
// =======================
// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// =======================
//  Build App
// =======================
var app = builder.Build();
// Usar CORS
app.UseCors("AllowAll");

// =======================
//  Middleware Configuration
// =======================
app.UseSwagger();
app.UseSwaggerUI();


//app.UseHttpsRedirection();

// =======================
// 8️⃣ Endpoint Mapping
// =======================

// Root endpoint
app.MapGet("/", () => "CrudCloudDb API is running! 🚀")
    .WithName("RootEndpoint")
    .WithOpenApi();

// Health check endpoint
app.MapGet("/health", () => new
    {
        status = "healthy",
        timestamp = DateTime.UtcNow,
        environment = app.Environment.EnvironmentName,
        version = "1.0.0"
    })
    .WithName("HealthCheck")
    .WithOpenApi();

// =======================
// 9️⃣ Run App
// =======================
app.Run();
