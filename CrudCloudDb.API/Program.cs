// =======================
// 1️⃣ Using Statements
// =======================
using CrudCloudDb.Infrastructure.Data;
using CrudCloudDb.Infrastructure.Services;
using CrudCloudDb.Infrastructure.Repositories;
using CrudCloudDb.Application.Services.Interfaces;
using CrudCloudDb.Application.Services.Implementation;
using CrudCloudDb.Application.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

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
// 6️⃣ Repositories Registration
// =======================
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IDatabaseInstanceRepository, DatabaseInstanceRepository>();

// TODO: Agregar cuando Miguel los implemente:
// builder.Services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
// builder.Services.AddScoped<IPlanRepository, PlanRepository>();

// =======================
// 7️⃣ Services Registration
// =======================

// Infrastructure Services (Docker, Email, etc.)
builder.Services.AddScoped<IMasterContainerService, MasterContainerService>();
builder.Services.AddScoped<IDockerService, DockerService>();
builder.Services.AddScoped<IPortManagerService, PortManagerService>();
builder.Services.AddScoped<ICredentialService, CredentialService>();
builder.Services.AddScoped<IEmailService, EmailService>();

// Application Services (Business Logic)
builder.Services.AddScoped<IDatabaseService, DatabaseService>();

// TODO: Agregar cuando Miguel los implemente:
// builder.Services.AddScoped<IAuthService, AuthService>();
// builder.Services.AddScoped<IJwtService, JwtService>();
// builder.Services.AddScoped<IPaymentService, PaymentService>();
// builder.Services.AddScoped<IPlanService, PlanService>();
// builder.Services.AddScoped<IUserService, UserService>();

// =======================
// 8️⃣ Controllers Configuration
// =======================
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Configuración para manejar enums como strings
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// =======================
// 9️⃣ Swagger Configuration
// =======================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "CrudCloudDb API",
        Version = "v1",
        Description = "API para gestión de bases de datos on-demand",
        Contact = new OpenApiContact
        {
            Name = "Voyager Team",
            Email = "info@voyager.com"
        }
    });

    // TODO: Configuración JWT para Swagger (cuando Miguel termine Auth)
    /*
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
    */
});

// =======================
// 🔟 Build App
// =======================
var app = builder.Build();

// =======================
// 1️⃣1️⃣ Middleware Configuration
// =======================

// CORS (siempre activo)
app.UseCors("AllowAll");

// Swagger (disponible en Development y Production)
if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "CrudCloudDb API v1");
        c.RoutePrefix = "swagger";
        c.DocumentTitle = "CrudCloudDb API Documentation";
    });
}

// HTTPS Redirect (solo en desarrollo local, no en Docker)
if (app.Environment.IsDevelopment() && !builder.Configuration.GetValue<bool>("IsDocker"))
{
    app.UseHttpsRedirection();
}

// TODO: Descomentar cuando Miguel termine Auth
// app.UseAuthentication();
// app.UseAuthorization();

// =======================
// 1️⃣2️⃣ Endpoint Mapping
// =======================

// Root endpoint
app.MapGet("/", () => Results.Ok(new
{
    message = "CrudCloudDb API is running! 🚀",
    environment = app.Environment.EnvironmentName,
    timestamp = DateTime.UtcNow,
    version = "1.0.0"
}))
    .WithName("RootEndpoint")
    .WithOpenApi()
    .WithTags("Health");

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    timestamp = DateTime.UtcNow,
    environment = app.Environment.EnvironmentName,
    version = "1.0.0",
    services = new
    {
        database = "connected",
        docker = "available"
    }
}))
    .WithName("HealthCheck")
    .WithOpenApi()
    .WithTags("Health");

// Mapear controllers
app.MapControllers();

// =======================
// 1️⃣3️⃣ Run App
// =======================
app.Run();