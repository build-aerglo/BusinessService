using BusinessService.Api.BackgroundServices;
using BusinessService.Application.Interfaces;
using BusinessService.Application.Services;
using BusinessService.Domain.Entities;
using BusinessService.Domain.Repositories;
using BusinessService.Infrastructure.Clients;
using BusinessService.Infrastructure.Context;
using BusinessService.Infrastructure.Repositories;
using BusinessService.Infrastructure.Utility;
using Dapper;


var builder = WebApplication.CreateBuilder(args);

Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

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
builder.Services.AddScoped<IBusinessSettingsRepository, BusinessSettingsRepository>();
builder.Services.AddScoped<ITagRepository, TagRepository>();

// Register application services (application layer)
builder.Services.AddScoped<IBusinessService, BusinessService.Application.Services.BusinessService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IBusinessSettingsService, BusinessSettingsService>();
builder.Services.AddScoped<IQrCodeService, QrCodeService>();
builder.Services.AddScoped<ITagService, TagService>();
SqlMapper.AddTypeHandler(new JsonTypeHandler<List<Faq>>());
SqlMapper.AddTypeHandler(new JsonTypeHandler<Dictionary<string, string>>());
SqlMapper.AddTypeHandler(new JsonTypeHandler<List<string>>());

// Register SearchService HttpClient producer
builder.Services.AddHttpClient<IBusinessSearchProducer, BusinessSearchHttpProducer>(client =>
{
    var searchServiceUrl = builder.Configuration["Services:SearchServiceUrl"];
    if (string.IsNullOrWhiteSpace(searchServiceUrl))
        throw new InvalidOperationException("Missing configuration: SearchServiceUrl");

    client.BaseAddress = new Uri(searchServiceUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// HTTP Client for UserService
builder.Services.AddHttpClient<IBusinessRepServiceClient, BusinessRepServiceClient>(client =>
{
    var userServiceUrl = builder.Configuration["Services:UserServiceUrl"];
    if (string.IsNullOrWhiteSpace(userServiceUrl))
        throw new InvalidOperationException("Missing configuration: UserServiceUrl");

    client.BaseAddress = new Uri(userServiceUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

builder.Services.AddHttpClient<IUserServiceClient, UserServiceClient>(client =>
{
    var userServiceUrl = builder.Configuration["Services:UserServiceUrl"];
    if (string.IsNullOrWhiteSpace(userServiceUrl))
        throw new InvalidOperationException("Missing configuration: UserServiceUrl");
    client.BaseAddress = new Uri(userServiceUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    
});

builder.Services.AddHostedService<DndModeExpiryBackgroundService>();
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
