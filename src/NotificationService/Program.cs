using System.Text.Json.Serialization;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using NotificationService.Consumers;
using NotificationService.Data;
using NotificationService.Options;
using NotificationService.Services;
using NotificationService.Services.Background;
using NotificationService.Services.NotificationRequests;
using NotificationService.Services.Notifications;
using Shared.Auth;
using Shared.Data;
using Shared.Middleware;
using Shared.Web;

var builder = WebApplication.CreateBuilder(args);

// HttpContext accessor（ICurrentUserAccessor 依賴）
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserAccessor, HttpContextUserAccessor>();

// PostgreSQL + EF Core（snake_case 命名慣例）
builder.Services.AddDbContext<NotificationDbContext>(opts =>
    opts.UseOpenJamNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// MassTransit + RabbitMQ（Outbox 寄信事件發布 + 消費上架 / 追蹤 / 註冊事件）
builder.Services.AddMassTransit(x =>
{
    void UseRetry(IRetryConfigurator r) =>
        r.Exponential(
            retryLimit:    5,
            minInterval:   TimeSpan.FromSeconds(1),
            maxInterval:   TimeSpan.FromSeconds(30),
            intervalDelta: TimeSpan.FromSeconds(2));

    x.AddConsumer<CatalogPublishedConsumer>(cfg => cfg.UseMessageRetry(UseRetry));
    x.AddConsumer<StoreFollowerChangedConsumer>(cfg => cfg.UseMessageRetry(UseRetry));
    x.AddConsumer<UserRegisteredConsumer>(cfg => cfg.UseMessageRetry(UseRetry));

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

// 強型別設定（Options pattern）
builder.Services.Configure<ServiceOptions>(builder.Configuration.GetSection("Services"));
builder.Services.Configure<NotificationOptions>(builder.Configuration.GetSection("Notification"));

// StoreService API client（Owner 驗證 + 商店公開資訊查詢）
var services = builder.Configuration.GetSection("Services").Get<ServiceOptions>() ?? new ServiceOptions();
var storeBaseUrl = (services.StoreService.BaseUrl ?? "http://localhost:5172").TrimEnd('/') + "/";
builder.Services.AddHttpClient("store", client => client.BaseAddress = new Uri(storeBaseUrl));
builder.Services.AddScoped<StoreServiceClient>();

// 業務邏輯 Service 層（Controller 僅負責 HTTP 轉接）
builder.Services.AddScoped<INotificationManager, NotificationManager>();
builder.Services.AddScoped<INotificationRequestService, NotificationRequestService>();

// 背景服務：通知任務 fan-out + Outbox → RabbitMQ
builder.Services.AddHostedService<NotificationDispatcherService>();
builder.Services.AddHostedService<OutboxRelayService>();

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
builder.Services.AddOpenJamSwagger("NotificationService");

// FluentValidation（model 驗證）+ AutoMapper（model 轉換）
builder.Services.AddOpenJamValidation(typeof(Program).Assembly);
builder.Services.AddOpenJamMapping(typeof(Program).Assembly);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider
        .GetRequiredService<NotificationDbContext>()
        .Database.MigrateAsync();
}

// PathBase：剝除 Ingress 轉發保留的服務前綴（如 /notification-service），須為第一個 middleware
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
