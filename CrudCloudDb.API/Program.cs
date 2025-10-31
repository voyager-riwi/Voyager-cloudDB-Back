// Deployment test - 2025-10-28

// =======================
// 1️⃣ Using Statements
// =======================
using System.Text;
using CrudCloudDb.Application.Interfaces.Repositories;
using CrudCloudDb.Application.Services.Implementation;
using CrudCloudDb.Application.Services.Interfaces;
using CrudCloudDb.Infrastructure.Data;
using CrudCloudDb.Infrastructure.Services;
using CrudCloudDb.Infrastructure.Repositories;
using CrudCloudDb.Infrastructure.Jobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

// =======================
// 2️⃣ Builder Initialization
// =======================
var builder = WebApplication.CreateBuilder(args);
// Configurar para escuchar en puerto 8081 (Docker)
builder.WebHost.UseUrls("http://0.0.0.0:5191");

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

// =======================
// 4️⃣ Database Configuration
// =======================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString)
        .UseSnakeCaseNamingConvention());

// =======================
// 5️⃣ JWT Authentication Configuration
// =======================
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var jwtSettings = builder.Configuration.GetSection("JwtSettings");
    var secret = jwtSettings["Secret"];
    if (secret == null) 
        throw new ArgumentNullException(nameof(secret), "JWT Secret not configured.");

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
        ClockSkew = TimeSpan.Zero // Sin margen de tiempo extra
    };
});

builder.Services.AddAuthorization();

// =======================
// 6️⃣ Repositories Registration
// =======================
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IPlanRepository, PlanRepository>();
builder.Services.AddScoped<IDatabaseInstanceRepository, DatabaseInstanceRepository>(); // ⭐ AGREGADO
builder.Services.AddScoped<IEmailLogRepository, EmailLogRepository>(); 

// =======================
// 7️⃣ Services Registration
// =======================
// Auth Services (de Miguel)
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();

// Database Services (tuyos) ⭐ AGREGADOS
builder.Services.AddScoped<IMasterContainerService, MasterContainerService>();
builder.Services.AddScoped<IDockerService, DockerService>();
builder.Services.AddScoped<IDatabaseService, DatabaseService>();
builder.Services.AddScoped<IPortManagerService, PortManagerService>();
builder.Services.AddScoped<ICredentialService, CredentialService>();
builder.Services.AddScoped<IEmailService, EmailService>();

// Background Jobs
builder.Services.AddHostedService<DatabaseCleanupJob>(); // 🧹 Limpieza automática de BDs eliminadas (30 días)

// =======================
// 8️⃣ Controllers Configuration
// =======================
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Convertir enums a strings en JSON
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// =======================
// 9️⃣ Swagger Configuration
// =======================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Configuración básica
    options.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "CrudCloudDb API", 
        Version = "v1",
        Description = "API para gestión de bases de datos on-demand"
    });

    // Configuración JWT para Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme.\n\n" +
                      "Enter 'Bearer' [space] and then your token in the text input below.\n\n" +
                      "Example: \"Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...\"",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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
});

// =======================
// 🔟 Build App
// =======================
var app = builder.Build();

// =======================
// 1️⃣1️⃣ Middleware Configuration
// =======================
// ⚠️ IMPORTANTE: El orden del middleware es CRÍTICO

// CORS (siempre primero)
app.UseCors("AllowAll");

// Swagger (Development y Production)
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

// HTTPS Redirect (solo en desarrollo local sin Docker)
if (app.Environment.IsDevelopment() && !builder.Configuration.GetValue<bool>("IsDocker"))
{
    app.UseHttpsRedirection();
}

// ⭐ CRÍTICO: Authentication ANTES de Authorization
app.UseAuthentication(); 
app.UseAuthorization();

// =======================
// 1️⃣2️⃣ Endpoint Mapping
// =======================

// Root endpoint (público)
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

// Health endpoint (público)
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    timestamp = DateTime.UtcNow,
    environment = app.Environment.EnvironmentName,
    version = "1.0.0"
}))
.WithName("HealthCheck")
.WithOpenApi()
.WithTags("Health");

// Mapear todos los controllers
app.MapControllers();

// =======================
// 1️⃣3️⃣ Run App
// =======================
app.Run();