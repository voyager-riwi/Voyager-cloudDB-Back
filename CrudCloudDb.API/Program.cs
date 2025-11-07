// =======================
// 1️⃣ Using Statements
// =======================
using System.Text;
using CrudCloudDb.API.Configuration;
using CrudCloudDb.API.Middleware;
using CrudCloudDb.Application.Interfaces.Repositories;
using CrudCloudDb.Application.Services.Implementation;
using CrudCloudDb.Application.Services.Interfaces;
using CrudCloudDb.Infrastructure.Data;
using CrudCloudDb.Infrastructure.Services;
using CrudCloudDb.Infrastructure.Repositories;
using CrudCloudDb.Core.Entities;
using CrudCloudDb.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using MercadoPago.Config; 
using CrudCloudDb.Application.Configuration;


// =======================
// 2️⃣ Serilog Bootstrap Configuration
// =======================
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .Build())
    .CreateBootstrapLogger();

// =======================
// 2️⃣.1 MercadoPago Initial Configuration
// =======================
// Se configurará después de cargar .env en desarrollo
var tempConfig = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

// Esto se sobreescribirá después de cargar .env si es necesario
MercadoPagoConfig.AccessToken = tempConfig["MercadoPagoSettings:AccessToken"] ?? "placeholder";

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
    // 4️⃣.1 Load Environment Variables from .env file (Development)
    // =======================
    if (builder.Environment.IsDevelopment())
    {
        var envFilePath = Path.Combine(Directory.GetCurrentDirectory(), "..", ".env");
        if (File.Exists(envFilePath))
        {
            foreach (var line in File.ReadAllLines(envFilePath))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                    continue;
                
                var parts = line.Split('=', 2, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2)
                {
                    Environment.SetEnvironmentVariable(parts[0].Trim(), parts[1].Trim());
                }
            }
            Log.Information("✅ Loaded .env file for development");
        }
    }

    // =======================
    // 4️⃣.2 Configure MercadoPago from Environment Variables
    // =======================
    var mercadoPagoAccessToken = Environment.GetEnvironmentVariable("MERCADOPAGO_ACCESS_TOKEN")
                                ?? builder.Configuration["MercadoPagoSettings:AccessToken"]
                                ?? "placeholder";
    
    if (mercadoPagoAccessToken != "placeholder")
    {
        MercadoPagoConfig.AccessToken = mercadoPagoAccessToken;
        Log.Information("✅ MercadoPago configured");
    }
    else
    {
        Log.Warning("⚠️ MercadoPago AccessToken not configured");
    }

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
    // Leer variables de entorno con fallback a appsettings
    var dbHost = Environment.GetEnvironmentVariable("DB_HOST") 
                 ?? builder.Configuration["ConnectionStrings:Host"] 
                 ?? "localhost";
    var dbPort = Environment.GetEnvironmentVariable("DB_PORT") 
                 ?? builder.Configuration["ConnectionStrings:Port"] 
                 ?? "5432";
    var dbName = Environment.GetEnvironmentVariable("DB_NAME") 
                 ?? builder.Configuration["ConnectionStrings:Database"] 
                 ?? "crud_cloud_db";
    var dbUser = Environment.GetEnvironmentVariable("DB_USER") 
                 ?? builder.Configuration["ConnectionStrings:Username"] 
                 ?? "postgres";
    var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD") 
                     ?? builder.Configuration["ConnectionStrings:Password"] 
                     ?? "password";
    
    var connectionString = $"Host={dbHost};Database={dbName};Username={dbUser};Password={dbPassword};Port={dbPort}";
    
    Log.Information($"🗄️ Database: {dbUser}@{dbHost}:{dbPort}/{dbName}");
    
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
    builder.Services.AddScoped<ICredentialService, CredentialService>();
    builder.Services.AddScoped<IPaymentService, PaymentService>();
    builder.Services.AddScoped<IWebhookService, WebhookService>(); 
    builder.Services.AddScoped<IUserRepository, UserRepository>();
    builder.Services.AddScoped<IPlanRepository, PlanRepository>();
    builder.Services.AddScoped<IDatabaseInstanceRepository, DatabaseInstanceRepository>();
    builder.Services.AddScoped<IEmailLogRepository, EmailLogRepository>();
    builder.Services.AddScoped<ISubscriptionRepository, SubscriptionRepository>(); 
    
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IUserService, UserService>();
    builder.Services.AddScoped<IEmailService, EmailService>();
    builder.Services.AddScoped<IMasterContainerService, MasterContainerService>();
    builder.Services.AddScoped<IDockerService, DockerService>();
    builder.Services.AddScoped<IDatabaseService, DatabaseService>();
    builder.Services.AddScoped<IPortManagerService, PortManagerService>();
    builder.Services.AddScoped<ICredentialService, CredentialService>();
    
    // =======================
    // 9️⃣.1 Webhook configuration
    // =======================
    var urlTest = builder.Configuration.GetSection("WeebhookSettings")["DiscordUrl"];

// Si estás en .NET 6/7, puedes usar Console.WriteLine en Program.cs
    
    builder.Services.AddHttpClient();
    builder.Services.Configure<WebhookSettings>(options =>
    {
        // Leer de variables de entorno con fallback a appsettings
        options.DiscordUrl = Environment.GetEnvironmentVariable("DISCORD_WEBHOOK_URL") 
                            ?? builder.Configuration["WebhookSettings:DiscordUrl"]
                            ?? string.Empty;
    });
    builder.Services.AddScoped<IWebhookService, WebhookService>();
    
    
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
    // 🌱 Database Initialization & Seeding
    // =======================
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var dbContext = services.GetRequiredService<ApplicationDbContext>();
            var logger = services.GetRequiredService<ILogger<Program>>();

            logger.LogInformation("🌱 Initializing database data...");

            // Crear planes si no existen
            if (!dbContext.Plans.Any())
            {
                logger.LogInformation("📋 Creating default plans...");

                var freePlan = new Plan
                {
                    Id = Guid.NewGuid(),
                    PlanType = PlanType.Free,
                    Name = "Free Plan",
                    Price = 0,
                    DatabaseLimitPerEngine = 2
                };

                var premiumPlan = new Plan
                {
                    Id = Guid.NewGuid(),
                    PlanType = PlanType.Advanced,
                    Name = "Premium Plan",
                    Price = 9.99m,
                    DatabaseLimitPerEngine = 5
                };

                dbContext.Plans.AddRange(freePlan, premiumPlan);
                await dbContext.SaveChangesAsync();

                logger.LogInformation("✅ Plans created: Free (2 DBs/engine), Premium (5 DBs/engine)");
            }

            // Asignar plan Free a usuarios sin plan
            var freePlanId = dbContext.Plans.FirstOrDefault(p => p.PlanType == PlanType.Free)?.Id;
            if (freePlanId.HasValue)
            {
                // Obtener IDs de planes válidos
                var validPlanIds = dbContext.Plans.Select(p => p.Id).ToList();
                
                // Buscar usuarios sin plan válido (CurrentPlanId no está en la lista de planes válidos)
                var usersWithoutPlan = dbContext.Users
                    .Where(u => !validPlanIds.Contains(u.CurrentPlanId))
                    .ToList();
                
                if (usersWithoutPlan.Any())
                {
                    logger.LogInformation($"👥 Assigning Free plan to {usersWithoutPlan.Count} user(s) without valid plan...");
                    
                    foreach (var user in usersWithoutPlan)
                    {
                        logger.LogInformation($"   - Assigning Free plan to user: {user.Email} (CurrentPlanId was: {user.CurrentPlanId})");
                        user.CurrentPlanId = freePlanId.Value;
                    }
                    
                    await dbContext.SaveChangesAsync();
                    logger.LogInformation($"✅ Free plan assigned to {usersWithoutPlan.Count} user(s)");
                }
                else
                {
                    logger.LogInformation("ℹ️ All users already have valid plans assigned");
                }
            }

            logger.LogInformation("✅ Database initialization completed");
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "❌ Error during database initialization");
        }
    }

    // =======================
    // 1️⃣3️⃣ Middleware Configuration
    // =======================
    app.UseCors("AllowAll");
    app.UseMiddleware<ErrorHandlingMiddleware>();

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