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

// =======================
//  Middleware Configuration
// =======================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// =======================
//  Endpoint Mapping
// =======================
// Aquí irán tus endpoints personalizados
app.MapGet("/", () => "Hello World!")
    .WithName("RootEndpoint")
    .WithOpenApi();

// =======================
//  Run App
// =======================
app.Run();