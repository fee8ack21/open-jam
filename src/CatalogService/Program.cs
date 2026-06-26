using System.Text.Json.Serialization;
using CatalogService.Consumers;
using CatalogService.Data;
using CatalogService.Options;
using CatalogService.Services;
using CatalogService.Services.Background;
using CatalogService.Services.Catalogs;
using CatalogService.Services.CatalogVersions;
using CatalogService.Services.Categories;
using CatalogService.Services.Favorites;
using CatalogService.Services.Reviews;
using CatalogService.Services.Tags;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Shared.Auth;
using Shared.Data;
using Shared.Middleware;
using Shared.Web;

var builder = WebApplication.CreateBuilder(args);

// HttpContext accessor（ICurrentUserAccessor 依賴）
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserAccessor, HttpContextUserAccessor>();

// PostgreSQL + EF Core（snake_case 命名慣例）
builder.Services.AddDbContext<CatalogDbContext>(opts =>
    opts.UseOpenJamNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// MassTransit + RabbitMQ（Outbox 事件發布 + 消費 OrderCompletedEvent 累加銷量）
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<OrderCompletedConsumer>(cfg =>
    {
        cfg.UseMessageRetry(r => r.Exponential(
            retryLimit:    5,
            minInterval:   TimeSpan.FromSeconds(1),
            maxInterval:   TimeSpan.FromSeconds(30),
            intervalDelta: TimeSpan.FromSeconds(2)));
    });

    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMQ:Host"] ?? "localhost", "/", h =>
        {
            h.Username(builder.Configuration["RabbitMQ:Username"] ?? "guest");
            h.Password(builder.Configuration["RabbitMQ:Password"] ?? "guest");
        });

        cfg.ConfigureEndpoints(ctx);
    });
});

builder.Services.AddHostedService<OutboxRelayService>();

// 強型別設定（Options pattern）
builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection("Storage"));
builder.Services.Configure<ServiceOptions>(builder.Configuration.GetSection("Services"));

// 外部微服務 API client
var services = builder.Configuration.GetSection("Services").Get<ServiceOptions>() ?? new ServiceOptions();

var storageBaseUrl = (services.StorageService.BaseUrl ?? "http://localhost:5171").TrimEnd('/') + "/";
builder.Services.AddHttpClient("storage", client => client.BaseAddress = new Uri(storageBaseUrl));
builder.Services.AddScoped<StorageServiceClient>();

var storeBaseUrl = (services.StoreService.BaseUrl ?? "http://localhost:5172").TrimEnd('/') + "/";
builder.Services.AddHttpClient("store", client => client.BaseAddress = new Uri(storeBaseUrl));
builder.Services.AddScoped<StoreServiceClient>();

var quotaBaseUrl = (services.QuotaService.BaseUrl ?? "http://localhost:5177").TrimEnd('/') + "/";
builder.Services.AddHttpClient("quota", client => client.BaseAddress = new Uri(quotaBaseUrl));
builder.Services.AddScoped<QuotaServiceClient>();

var orderBaseUrl = (services.OrderService.BaseUrl ?? "http://localhost:5179").TrimEnd('/') + "/";
builder.Services.AddHttpClient("order", client => client.BaseAddress = new Uri(orderBaseUrl));
builder.Services.AddScoped<OrderServiceClient>();

// 業務邏輯 Service 層（Controller 僅負責 HTTP 轉接）
builder.Services.AddScoped<AuditLogPublisher>();
builder.Services.AddScoped<ICatalogManager, CatalogManager>();
builder.Services.AddScoped<ICatalogVersionService, CatalogVersionService>();
builder.Services.AddScoped<ICatalogCategoryService, CatalogCategoryService>();
builder.Services.AddScoped<ICatalogTagService, CatalogTagService>();
builder.Services.AddScoped<ICatalogFavoriteService, CatalogFavoriteService>();
builder.Services.AddScoped<ICatalogReviewService, CatalogReviewService>();

// JWT Bearer 驗證（Hydra JWKS）+ Admin Policy
builder.Services.AddOpenJamJwtAuth(builder.Configuration);

// CORS（允許前端 SPA 與本機 dev server 跨來源呼叫）
builder.Services.AddOpenJamCors(builder.Configuration);

// REST API
// enum 一律以名稱（字串）序列化，讓 OpenAPI 產出具名 enum，前端 codegen 不再是 Value0..N
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
        opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenJamApiVersioning();
builder.Services.AddOpenJamSwagger("CatalogService");

// FluentValidation（model 驗證）+ AutoMapper（model 轉換）
builder.Services.AddOpenJamValidation(typeof(Program).Assembly);
builder.Services.AddOpenJamMapping(typeof(Program).Assembly);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider
        .GetRequiredService<CatalogDbContext>()
        .Database.MigrateAsync();
}

// PathBase：剝除 Ingress 轉發保留的服務前綴（如 /catalog-service），須為第一個 middleware
app.UseOpenJamPathBase();

app.UseExceptionMiddleware();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseOpenJamCors();

app.UseAuthentication();
app.UseAuthorization();

// K8s liveness/readiness probe 用
app.MapGet("/healthz", () => Results.Ok());

app.MapControllers();
app.Run();
