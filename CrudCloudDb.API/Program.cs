// =======================
// 1️⃣ Using Statements
// =======================
using CrudCloudDb.API.Middleware; // Para nuestros middlewares
using CrudCloudDb.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models; // Para la configuración de Swagger
using Serilog; // Para Serilog

// =======================
//  Builder Initialization
// =======================
var builder = WebApplication.CreateBuilder(args);

// =======================
//  Serilog Configuration
// =======================
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/crudclouddb-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7)
    .CreateLogger();

builder.Host.UseSerilog();

// =======================
//  Service Configuration
// =======================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString)
        .UseSnakeCaseNamingConvention());

// Controllers
builder.Services.AddControllers();

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "CrudCloudDb API",
        Version = "v1",
        Description = "API para la plataforma de gestión de bases de datos en la nube."
    });

    // Configuración de JWT en Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Autorización JWT usando el esquema Bearer. Ingrese 'Bearer' [espacio] y luego su token.",
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
});


// =======================
//  Build App
// =======================
var app = builder.Build();

// ===================================
//  Middleware Configuration (IMPORTANTE EL ORDEN)
// ===================================

// 1. Middleware para manejo de errores (debe ser el primero)
app.UseMiddleware<ErrorHandlingMiddleware>();

// 2. Middleware de logging de peticiones de Serilog
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// app.UseAuthentication(); // Se configurará por Miguel
app.UseAuthorization();

// 3. Middleware de auditoría (después de auth para tener el contexto de usuario)
app.UseMiddleware<AuditMiddleware>();


// =======================
//  Endpoint Mapping
// =======================
app.MapControllers(); // Habilita el mapeo de los controllers

// =======================
//  Run App
// =======================
app.Run();