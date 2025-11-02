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

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ---------------------------------------------------------------------
// 2️⃣  Configure the HTTP request pipeline
// ---------------------------------------------------------------------

// 1️⃣ Swagger setup
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "User Service API v1");
    options.RoutePrefix = ""; // load Swagger at root
});

// Optional: Global exception handler middleware (recommended)
// app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();
app.UseCors("AllowAll");

// Enable attribute-routed controllers
app.MapControllers();

app.Run();
