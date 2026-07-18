using System.Text.Json.Serialization;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Shared.Auth;
using Shared.Data;
using Shared.Middleware;
using Shared.Web;
using StoreService.Consumers;
using StoreService.Data;
using StoreService.Options;
using StoreService.Services;
using StoreService.Services.Background;
using StoreService.Services.StoreApplications;
using StoreService.Services.StoreFollowers;
using StoreService.Services.Stores;

var builder = WebApplication.CreateBuilder(args);

// HttpContext accessor（ICurrentUserAccessor 依賴）
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserAccessor, HttpContextUserAccessor>();

// PostgreSQL + EF Core（snake_case 命名慣例）
builder.Services.AddDbContext<StoreDbContext>(opts =>
    opts.UseOpenJamNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// MassTransit + RabbitMQ（Outbox 事件發布 + 消費 UserRegisteredEvent 回填追蹤者 UserId）
builder.Services.AddMassTransit(x =>
{
    // queue 名稱加服務前綴：跨服務同名 consumer（如 UserRegisteredConsumer）才不會綁到同一條
    // queue 變成輪流分食，廣播事件每個服務必須各收一份。
    x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("Store", false));

    x.AddConsumer<UserRegisteredConsumer>(cfg =>
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

// StorageService API client（簽發 Avatar/Banner 上傳 URL）
var services = builder.Configuration.GetSection("Services").Get<ServiceOptions>() ?? new ServiceOptions();
// 呼叫 StorageService 內部檔案 API 需帶 service token（受 InternalService policy 保護）。
builder.Services.AddOpenJamServiceTokenClient(builder.Configuration);

var storageBaseUrl = (services.StorageService.BaseUrl ?? "http://localhost:5171").TrimEnd('/') + "/";
builder.Services.AddHttpClient("storage", client => client.BaseAddress = new Uri(storageBaseUrl))
    .AddHttpMessageHandler<ServiceTokenHandler>();
builder.Services.AddScoped<StorageServiceClient>();

// 業務邏輯 Service 層（Controller 僅負責 HTTP 轉接）
builder.Services.AddScoped<AuditLogPublisher>();
builder.Services.AddScoped<StoreEventPublisher>();
builder.Services.AddScoped<IStoreManager, StoreManager>();
builder.Services.AddScoped<IStoreApplicationService, StoreApplicationService>();
builder.Services.AddScoped<IStoreFollowerService, StoreFollowerService>();

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
builder.Services.AddOpenJamSwagger("StoreService");

// FluentValidation（model 驗證）+ AutoMapper（model 轉換）
builder.Services.AddOpenJamValidation(typeof(Program).Assembly);
builder.Services.AddOpenJamMapping(typeof(Program).Assembly);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider
        .GetRequiredService<StoreDbContext>()
        .Database.MigrateAsync();
}

// PathBase：剝除 Ingress 轉發保留的服務前綴（如 /store-service），須為第一個 middleware
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
