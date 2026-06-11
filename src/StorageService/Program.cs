using MassTransit;
using Microsoft.EntityFrameworkCore;
using Minio;
using Shared.Auth;
using Shared.Middleware;
using StorageService.Data;
using StorageService.Options;
using StorageService.Services;
using StorageService.Storage;

var builder = WebApplication.CreateBuilder(args);

// 設定解析
var storageOpts = builder.Configuration.GetSection("Storage").Get<StorageOptions>()
    ?? throw new InvalidOperationException("Storage 設定缺失");

// PostgreSQL + EF Core（snake_case 命名慣例）
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserAccessor, HttpContextUserAccessor>();
builder.Services.AddDbContext<StorageDbContext>(opts =>
    opts.UseNpgsql(builder.Configuration.GetConnectionString("Postgres"),
            o => o.MigrationsHistoryTable("__ef_migrations_history"))
        .UseSnakeCaseNamingConvention());

// MinIO
builder.Services.AddSingleton<IMinioClient>(_ =>
    new MinioClient()
        .WithEndpoint(storageOpts.Endpoint)
        .WithCredentials(storageOpts.AccessKey, storageOpts.SecretKey)
        .WithSSL(storageOpts.UseSsl)
        .Build());

builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection("Storage"));
builder.Services.AddScoped<IStorageProvider, MinioStorageProvider>();

// MassTransit + RabbitMQ（publish only，FileReadyEvent）
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

// 業務服務
builder.Services.AddScoped<FileProcessingService>();
builder.Services.AddSingleton<OrphanCleanupService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<OrphanCleanupService>());

// REST API
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opts =>
{
    opts.SwaggerDoc("v1", new() { Title = "StorageService", Version = "v1" });
    var xmlPath = Path.Combine(AppContext.BaseDirectory, "StorageService.xml");
    if (File.Exists(xmlPath)) opts.IncludeXmlComments(xmlPath);
});

// JWT Bearer 驗證（Hydra JWKS）+ Admin Policy
builder.Services.AddOpenJamJwtAuth(builder.Configuration);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider
        .GetRequiredService<StorageDbContext>()
        .Database.MigrateAsync();

    await scope.ServiceProvider
        .GetRequiredService<IStorageProvider>()
        .EnsurePublicReadPolicyAsync();
}

app.UseExceptionMiddleware();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();
