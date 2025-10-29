// =======================
// 1️⃣ Using Statements
// =======================
using CrudCloudDb.Infrastructure.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// =======================
// 2️⃣ Builder Initialization
// =======================
var builder = WebApplication.CreateBuilder(args);

// =======================
// 3️⃣ Port Configuration (Flexible)
// =======================
// Lee el puerto de configuración o usa default
var port = builder.Configuration.GetValue<string>("AppSettings:Port") ?? "8080";
var host = builder.Configuration.GetValue<string>("AppSettings:Host") ?? "localhost";

// Solo configura puerto en producción (Docker)
if (builder.Environment.IsProduction())
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

// =======================
// 4️⃣ CORS Configuration
// =======================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// =======================
// 5️⃣ Database Configuration
// =======================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString)
        .UseSnakeCaseNamingConvention());

// =======================
// 6️⃣ Service Configuration
// =======================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "CrudCloudDb API",
        Version = "v1",
        Description = "API para gestión de bases de datos on-demand"
    });
});

// TODO: Aquí irán los servicios de Miguel y tuyos cuando estén listos
// builder.Services.AddScoped<IDockerService, DockerService>();
// builder.Services.AddScoped<IAuthService, AuthService>();
// etc...

// =======================
// 7️⃣ Build App
// =======================
var app = builder.Build();

// =======================
// 8️⃣ Middleware Configuration
// =======================

// CORS (siempre activo)
app.UseCors("AllowAll");

// Swagger (configuración flexible por ambiente)
if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "CrudCloudDb API v1");
        c.RoutePrefix = "swagger";
    });
}

// HTTPS Redirect (solo en desarrollo local, no en Docker)
if (app.Environment.IsDevelopment() && !builder.Configuration.GetValue<bool>("IsDocker"))
{
    app.UseHttpsRedirection();
}

// =======================
// 9️⃣ Endpoint Mapping
// =======================

// Root endpoint
app.MapGet("/", () => Results.Ok(new
{
    message = "CrudCloudDb API is running! 🚀",
    environment = app.Environment.EnvironmentName,
    timestamp = DateTime.UtcNow
}))
    .WithName("RootEndpoint")
    .WithOpenApi();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    timestamp = DateTime.UtcNow,
    environment = app.Environment.EnvironmentName,
    version = "1.0.0"
}))
    .WithName("HealthCheck")
    .WithOpenApi();

// TODO: Aquí irán los controllers cuando los agreguen
// app.MapControllers();

// =======================
// 🔟 Run App
// =======================
app.Run();
