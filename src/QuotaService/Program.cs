using System.Text.Json.Serialization;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Shared.Data;
using QuotaService.Consumers;
using QuotaService.Data;
using QuotaService.Options;
using QuotaService.Services;
using QuotaService.Services.Background;
using QuotaService.Services.Quotas;
using Shared.Auth;
using Shared.Middleware;
using Shared.Web;

var builder = WebApplication.CreateBuilder(args);

// HttpContext accessor（ICurrentUserAccessor 依賴）
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserAccessor, HttpContextUserAccessor>();

// PostgreSQL + EF Core（snake_case 命名慣例）
builder.Services.AddDbContext<QuotaDbContext>(opts =>
    opts.UseOpenJamNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// MassTransit + RabbitMQ（consumer：FileReadyEvent → commit 預扣）
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<FileReadyConsumer>(cfg =>
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

// 強型別設定（Options pattern）
builder.Services.Configure<QuotaOptions>(builder.Configuration.GetSection("Quota"));
builder.Services.Configure<ServiceOptions>(builder.Configuration.GetSection("Services"));

// 外部微服務 API client（對帳加總實際用量）
var services = builder.Configuration.GetSection("Services").Get<ServiceOptions>() ?? new ServiceOptions();
var storageBaseUrl = (services.StorageService.BaseUrl ?? "http://localhost:5171").TrimEnd('/') + "/";
builder.Services.AddHttpClient("storage", client => client.BaseAddress = new Uri(storageBaseUrl));
builder.Services.AddScoped<StorageServiceClient>();

// 業務邏輯 Service 層（Controller 僅負責 HTTP 轉接）
builder.Services.AddScoped<IQuotaManager, QuotaManager>();

// 背景服務：逾時預扣 sweeper + 每日對帳
builder.Services.AddHostedService<ReservationSweeperService>();
builder.Services.AddHostedService<ReconciliationService>();

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
builder.Services.AddOpenJamSwagger("QuotaService");

// FluentValidation（model 驗證）+ AutoMapper（model 轉換）
builder.Services.AddOpenJamValidation(typeof(Program).Assembly);
builder.Services.AddOpenJamMapping(typeof(Program).Assembly);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider
        .GetRequiredService<QuotaDbContext>()
        .Database.MigrateAsync();
}

// PathBase：剝除 Ingress 轉發保留的服務前綴（如 /quota-service），須為第一個 middleware
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
