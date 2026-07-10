using System.Text.Json.Serialization;
using ContentService.Data;
using ContentService.Services;
using ContentService.Services.Background;
using ContentService.Services.FaqCategories;
using ContentService.Services.Faqs;
using ContentService.Services.Legal;
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
builder.Services.AddDbContext<ContentDbContext>(opts =>
    opts.UseOpenJamNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// MassTransit + RabbitMQ（Outbox 事件發布；本服務僅發布 AuditLogRequestedEvent，不消費事件）
builder.Services.AddMassTransit(x =>
{
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

// 業務邏輯 Service 層（Controller 僅負責 HTTP 轉接）
builder.Services.AddScoped<AuditLogPublisher>();
builder.Services.AddScoped<ILegalDocumentService, LegalDocumentService>();
builder.Services.AddScoped<IFaqCategoryService, FaqCategoryService>();
builder.Services.AddScoped<IFaqService, FaqService>();

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
builder.Services.AddOpenJamSwagger("ContentService");

// FluentValidation（model 驗證）+ AutoMapper（model 轉換）
builder.Services.AddOpenJamValidation(typeof(Program).Assembly);
builder.Services.AddOpenJamMapping(typeof(Program).Assembly);

var app = builder.Build();

// 產生 OpenAPI 文件（dotnet swagger tofile / 離線 swagger 匯出）等情境下，略過需連線外部資源的啟動程序。
var skipStartupTasks = Environment.GetEnvironmentVariable("OPENJAM_SKIP_STARTUP_TASKS") == "true";

if (!skipStartupTasks)
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider
        .GetRequiredService<ContentDbContext>()
        .Database.MigrateAsync();
}

// PathBase：剝除 Ingress 轉發保留的服務前綴（如 /content-service），須為第一個 middleware
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
