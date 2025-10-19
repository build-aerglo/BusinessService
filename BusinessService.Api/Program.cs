using BusinessService.Application.Interfaces;
using BusinessService.Application.Services;
using BusinessService.Domain.Repositories;
using BusinessService.Infrastructure.Context;
using BusinessService.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------
// 1️⃣  Add services to the container
// ---------------------------------------------------------------------

// Enable controllers (for attribute routing)
builder.Services.AddControllers();

// Add OpenAPI (Swagger)
builder.Services.AddOpenApi();

// Register Dapper context (singleton since it only manages connection strings)
builder.Services.AddSingleton<DapperContext>();

// Register repositories (infrastructure layer)
builder.Services.AddScoped<IBusinessRepository, BusinessRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();

// Register application services (application layer)
builder.Services.AddScoped<IBusinessService, BusinessService.Application.Services.BusinessService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();

// Optional: CORS (if calling from frontend)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});

var app = builder.Build();

// ---------------------------------------------------------------------
// 2️⃣  Configure the HTTP request pipeline
// ---------------------------------------------------------------------

if (app.Environment.IsDevelopment())
{
    // Enable Swagger UI
    app.MapOpenApi(); // new .NET 9 style
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Business Service API v1");
        options.RoutePrefix = string.Empty; // Swagger at root URL
    });
}

// Optional: Global exception handler middleware (recommended)
// app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();
app.UseCors("AllowAll");

// Enable attribute-routed controllers
app.MapControllers();

app.Run();
