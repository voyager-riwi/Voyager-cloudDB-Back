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
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using ILogger = Microsoft.Extensions.Logging.ILogger;

// =======================
// 2️⃣ Serilog Bootstrap Configuration
// =======================
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .Build())
    .CreateBootstrapLogger();


// =======================
// 3️⃣ Main Application Block
// =======================

try
{
    Log.Information("Configuring web host...");

    // =======================
    // 4️⃣ Builder Initialization
    // =======================
    var builder = WebApplication.CreateBuilder(args);

    // =======================
    // 5️⃣ Serilog Full Integration
    // =======================
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    // =======================
    // 6️⃣ CORS Configuration
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
    // 7️⃣ Database Configuration
    // =======================
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention());

    // =======================
    // 8️⃣ JWT Authentication Configuration
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
            ClockSkew = TimeSpan.Zero
        };
    });

    builder.Services.AddAuthorization();

    // =======================
    // 9️⃣ Repositories & Services Registration
    // =======================
    builder.Services.AddScoped<IUserRepository, UserRepository>();
    builder.Services.AddScoped<IPlanRepository, PlanRepository>();
    builder.Services.AddScoped<IDatabaseInstanceRepository, DatabaseInstanceRepository>();
    builder.Services.AddScoped<IEmailLogRepository, EmailLogRepository>();

    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IUserService, UserService>();
    builder.Services.AddScoped<IEmailService, EmailService>();
    builder.Services.AddScoped<IMasterContainerService, MasterContainerService>();
    builder.Services.AddScoped<IDockerService, DockerService>();
    builder.Services.AddScoped<IDatabaseService, DatabaseService>();
    builder.Services.AddScoped<IPortManagerService, PortManagerService>();
    builder.Services.AddScoped<ICredentialService, CredentialService>();
    

    // =======================
    // 🔟 Controllers Configuration
    // =======================
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(
                new System.Text.Json.Serialization.JsonStringEnumConverter());
        });

    // =======================
    // 1️⃣1️⃣ Swagger Configuration
    // =======================
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo 
        { 
            Title = "CrudCloudDb API", 
            Version = "v1",
            Description = "API para gestión de bases de datos on-demand"
        });

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
    // 1️⃣2️⃣ Build App
    // =======================
    var app = builder.Build();

    // =======================
    // 1️⃣3️⃣ Middleware Configuration
    // =======================
    app.UseCors("AllowAll");

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

    if (app.Environment.IsDevelopment() && !builder.Configuration.GetValue<bool>("IsDocker"))
    {
        app.UseHttpsRedirection();
    }
    
    app.UseSerilogRequestLogging(); 

    app.UseAuthentication(); 
    app.UseAuthorization();

    // =======================
    // 1️⃣4️⃣ Endpoint Mapping
    // =======================
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

    app.MapControllers();

    // =======================
    // 1️⃣5️⃣ Run App
    // =======================
    Log.Information("Starting web host..."); 
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly."); 
}
finally
{
    Log.CloseAndFlush(); 
}