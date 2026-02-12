using BusinessService.Api.BackgroundServices;
using BusinessService.Application.Interfaces;
using BusinessService.Application.Services;
using BusinessService.Domain.Entities;
using BusinessService.Domain.Repositories;
using BusinessService.Infrastructure.Clients;
using BusinessService.Infrastructure.Context;
using BusinessService.Infrastructure.Repositories;
using BusinessService.Infrastructure.PaymentInitiators.Paystack;
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

// New repositories for business service features
builder.Services.AddScoped<IBusinessVerificationRepository, BusinessVerificationRepository>();
builder.Services.AddScoped<IIdVerificationRequestRepository, IdVerificationRequestRepository>();
builder.Services.AddScoped<ISubscriptionPlanRepository, SubscriptionPlanRepository>();
builder.Services.AddScoped<IBusinessSubscriptionRepository, BusinessSubscriptionRepository>();
builder.Services.AddScoped<IBusinessUserRepository, BusinessUserRepository>();
builder.Services.AddScoped<IAutoResponseTemplateRepository, AutoResponseTemplateRepository>();
builder.Services.AddScoped<IBusinessClaimRequestRepository, BusinessClaimRequestRepository>();
builder.Services.AddScoped<IExternalSourceRepository, ExternalSourceRepository>();
builder.Services.AddScoped<IBusinessAnalyticsRepository, BusinessAnalyticsRepository>();
builder.Services.AddScoped<IBranchComparisonRepository, BranchComparisonRepository>();
builder.Services.AddScoped<ICompetitorComparisonRepository, CompetitorComparisonRepository>();
builder.Services.AddScoped<ICompetitorComparisonSnapshotRepository, CompetitorComparisonSnapshotRepository>();
builder.Services.AddScoped<IBusinessAutoResponseRepository, BusinessAutoResponseRepository>();
builder.Services.AddScoped<ISubscriptionInvoiceRepository, SubscriptionInvoiceRepository>();

// Register application services (application layer)
builder.Services.AddScoped<IBusinessService, BusinessService.Application.Services.BusinessService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IBusinessSettingsService, BusinessSettingsService>();
builder.Services.AddScoped<IQrCodeService, QrCodeService>();
builder.Services.AddScoped<ITagService, TagService>();

// New services for business service features
builder.Services.AddScoped<IBusinessVerificationService, BusinessVerificationService>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddScoped<IBusinessUserService, BusinessUserService>();
builder.Services.AddScoped<IAutoResponseService, AutoResponseService>();
builder.Services.AddScoped<IBusinessClaimService, BusinessClaimService>();
builder.Services.AddScoped<IExternalSourceService, ExternalSourceService>();
builder.Services.AddScoped<IBusinessAnalyticsService, BusinessAnalyticsService>();
builder.Services.AddScoped<IBusinessAutoResponseService, BusinessAutoResponseService>();
builder.Services.AddScoped<ISubscriptionInvoiceService, SubscriptionInvoiceService>();

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

builder.Services.AddHttpClient<IPaymentInitiator, PaystackPaymentInitiator>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

builder.Services.AddHttpClient<INotificationServiceClient, NotificationServiceClient>(client =>
{
    var notificationServiceUrl = builder.Configuration["Services:NotificationServiceUrl"];
    if (string.IsNullOrWhiteSpace(notificationServiceUrl))
        throw new InvalidOperationException("Missing configuration: NotificationServiceUrl");

    client.BaseAddress = new Uri(notificationServiceUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

builder.Services.AddHostedService<DndModeExpiryBackgroundService>();
builder.Services.AddHostedService<BusinessUpdateListener>();
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
