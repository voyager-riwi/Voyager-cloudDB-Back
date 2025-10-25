// =======================
// 1️⃣ Using Statements
// =======================
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// =======================
// 2️⃣ Builder Initialization
// =======================
var builder = WebApplication.CreateBuilder(args);

// =======================
// 3️⃣ Service Configuration
// =======================
// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// =======================
// 4️⃣ Build App
// =======================
var app = builder.Build();

// =======================
// 5️⃣ Middleware Configuration
// =======================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// =======================
// 6️⃣ Endpoint Mapping
// =======================
// Aquí irán tus endpoints personalizados
app.MapGet("/", () => "Hello World!")
    .WithName("RootEndpoint")
    .WithOpenApi();

// =======================
// 7️⃣ Run App
// =======================
app.Run();