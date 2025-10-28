using CrudCloudDb.API.Middleware;
using CrudCloudDb.Application.Interfaces.Repositories; // <-- Añade este using
using CrudCloudDb.Application.Services.Interfaces;    // <-- Añade este using
using CrudCloudDb.Infrastructure.Data;
using CrudCloudDb.Infrastructure.Repositories;      // <-- Añade este using
using CrudCloudDb.Infrastructure.Services;          // <-- Añade este using
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/crudclouddb-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString)
        .UseSnakeCaseNamingConvention());

builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "PostgreSQL Health Check");

builder.Services.AddControllers();

builder.Services.AddScoped<IPlanRepository, PlanRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
builder.Services.AddScoped<IEmailLogRepository, EmailLogRepository>();

builder.Services.AddScoped<IEmailService, EmailService>();

builder.Services.AddScoped<ICredentialService, CredentialService>();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "CrudCloudDb API",
        Version = "v1",
        Description = "API para la plataforma de gestión de bases de datos en la nube."
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Autenticación JWT con Bearer token. Formato: 'Bearer {token}'",
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

var app = builder.Build();

app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseSerilogRequestLogging();
app.UseMiddleware<AuditMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();