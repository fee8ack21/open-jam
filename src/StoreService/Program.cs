using MassTransit;
using Microsoft.EntityFrameworkCore;
using Shared.Auth;
using Shared.Middleware;
using StoreService.Data;
using StoreService.Services;

var builder = WebApplication.CreateBuilder(args);

// HttpContext accessor（ICurrentUserAccessor 依賴）
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserAccessor, HttpContextUserAccessor>();

// PostgreSQL + EF Core（snake_case 命名慣例）
builder.Services.AddDbContext<StoreDbContext>(opts =>
    opts.UseNpgsql(builder.Configuration.GetConnectionString("Postgres"),
            o => o.MigrationsHistoryTable("__ef_migrations_history"))
        .UseSnakeCaseNamingConvention());

// MassTransit + RabbitMQ（publish only，Outbox 事件）
builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((_, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMQ:Host"] ?? "localhost", "/", h =>
        {
            h.Username(builder.Configuration["RabbitMQ:Username"] ?? "guest");
            h.Password(builder.Configuration["RabbitMQ:Password"] ?? "guest");
        });
    });
});

builder.Services.AddHostedService<OutboxRelayService>();

// StorageService API client（簽發 Avatar/Banner 上傳 URL）
var storageBaseUrl = (builder.Configuration["Services:StorageService:BaseUrl"] ?? "http://localhost:5171").TrimEnd('/') + "/";
builder.Services.AddHttpClient("storage", client => client.BaseAddress = new Uri(storageBaseUrl));
builder.Services.AddScoped<StorageServiceClient>();

// JWT Bearer 驗證（Hydra JWKS）+ Admin Policy
builder.Services.AddOpenJamJwtAuth(builder.Configuration);

// REST API
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opts =>
{
    opts.SwaggerDoc("v1", new() { Title = "StoreService", Version = "v1" });
    var xmlPath = Path.Combine(AppContext.BaseDirectory, "StoreService.xml");
    if (File.Exists(xmlPath)) opts.IncludeXmlComments(xmlPath);
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider
        .GetRequiredService<StoreDbContext>()
        .Database.MigrateAsync();
}

app.UseExceptionMiddleware();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

// K8s liveness/readiness probe 用
app.MapGet("/healthz", () => Results.Ok());

app.MapControllers();
app.Run();
