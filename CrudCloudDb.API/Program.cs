// =======================
// 1️⃣ Using Statements
// =======================

using System.Text;
using CrudCloudDb.Application.Interfaces.Repositories;
using CrudCloudDb.Application.Services.Implementation;
using CrudCloudDb.Application.Services.Interfaces;
using CrudCloudDb.Infrastructure.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using CrudCloudDb.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

// =======================
//  Builder Initialization
// =======================
var builder = WebApplication.CreateBuilder(args);


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
//  JWT configuration 
// =======================

builder.Services.AddAuthentication(options =>
    {
        // Le decimos que se encargara de validar credenciales
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    //damos las instrucciones
    .AddJwtBearer(options =>
    {
        var jwtSettings = builder.Configuration.GetSection("JwtSettings");
        var secret = jwtSettings["Secret"];
        if(secret == null) throw new ArgumentNullException(nameof(secret), "JWT Secret not configured.");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true, //revisa lo mas importante, osea la clave
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))
        };
    });

// =======================
// 6️⃣ Service Configuration
// =======================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Configuración básica de Swagger
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "CrudCloudDb.API", Version = "v1" });

    // Pantallita para definir el esquema, para introducir el token
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Por favor, introduce 'Bearer' seguido de un espacio y el token JWT",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    // Le decimos a Swagger que aplique este requisito de seguridad a las operaciones
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
            new string[] {}
        }
    });
});

builder.Services.AddControllers();

// TODO: Aquí irán los servicios
// builder.Services.AddScoped<IDockerService, DockerService>();

// Registro de repositorios
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IPlanRepository, PlanRepository>();
//builder.Services.AddScoped<IDatabaseInstanceRepository, DatabaseInstanceRepository>();

// Registro de servicios
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();

// =======================
// 7️⃣ Build App
// =======================
var app = builder.Build();

// =======================
// 8️⃣ Middleware Configuration
// =======================
// ¡OJO! El orden del middleware es MUY importante.

app.UseCors("AllowAll");

// Swagger (configuración flexible por ambiente)
if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// HTTPS Redirect (flexible)
if (app.Environment.IsDevelopment() && !builder.Configuration.GetValue<bool>("IsDocker"))
{
    app.UseHttpsRedirection();
}

// Autenticación y Autorización - DEBEN ir DESPUÉS de CORS y ANTES de MapControllers
app.UseAuthentication(); 
app.UseAuthorization();

// =======================
// 9️⃣ Endpoint Mapping
// =======================

app.MapGet("/", () => Results.Ok(new
{
    message = "CrudCloudDb API is running! 🚀",
    environment = app.Environment.EnvironmentName,
    timestamp = DateTime.UtcNow
}))
    .WithName("RootEndpoint")
    .WithOpenApi();

app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    timestamp = DateTime.UtcNow,
    environment = app.Environment.EnvironmentName,
    version = "1.0.0"
}))
    .WithName("HealthCheck")
    .WithOpenApi();

app.MapControllers();

// =======================
// 🔟 Run App
// =======================
app.Run();