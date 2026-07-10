using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Shared.Auth;
using Shared.Data;
using Shared.Middleware;
using Shared.Web;
using StorageService.Data;
using StorageService.Options;
using StorageService.Services;
using StorageService.Services.Files;
using StorageService.Services.StorageEvents;
using StorageService.Storage;

var builder = WebApplication.CreateBuilder(args);

// 設定解析
var storageOpts = builder.Configuration.GetSection("Storage").Get<StorageOptions>()
    ?? throw new InvalidOperationException("Storage 設定缺失");

// PostgreSQL + EF Core（snake_case 命名慣例）
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserAccessor, HttpContextUserAccessor>();
builder.Services.AddDbContext<StorageDbContext>(opts =>
    opts.UseOpenJamNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection("Storage"));

// 儲存後端：地端本地檔案系統 / 雲端 Google Cloud Storage，依設定切換
if (storageOpts.Provider == StorageProvider.Gcs)
{
    // 服務帳戶金鑰可簽章 signed URL；留空則用 ADC（GKE Workload Identity，透過 IAM SignBlob 簽章）
    var credential = string.IsNullOrWhiteSpace(storageOpts.Gcs.CredentialsPath)
        ? GoogleCredential.GetApplicationDefault()
        : CredentialFactory.FromFile<ServiceAccountCredential>(storageOpts.Gcs.CredentialsPath)
            .ToGoogleCredential();

    builder.Services.AddSingleton(StorageClient.Create(credential));
    builder.Services.AddSingleton(UrlSigner.FromCredential(credential));
    builder.Services.AddScoped<IStorageProvider, GcsStorageProvider>();
}
else
{
    // 本地檔案儲存：blob URL 由 BlobUrlSigner 以 HMAC 簽章，實體檔案由 LocalFileStore 讀寫。
    builder.Services.AddSingleton<LocalFileStore>();
    builder.Services.AddSingleton<BlobUrlSigner>();
    builder.Services.AddScoped<IStorageProvider, LocalStorageProvider>();
}

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
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<IStorageEventService, StorageEventService>();
builder.Services.AddScoped<FileProcessingService>();
builder.Services.AddSingleton<OrphanCleanupService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<OrphanCleanupService>());

// REST API
builder.Services.AddControllers();
builder.Services.AddOpenJamApiVersioning();
builder.Services.AddOpenJamSwagger("StorageService");

// FluentValidation（model 驗證）+ AutoMapper（model 轉換）
builder.Services.AddOpenJamValidation(typeof(Program).Assembly);
builder.Services.AddOpenJamMapping(typeof(Program).Assembly);

// JWT Bearer 驗證（Hydra JWKS）+ Admin Policy
builder.Services.AddOpenJamJwtAuth(builder.Configuration);

// CORS（允許前端 SPA 與本機 dev server 跨來源呼叫）
builder.Services.AddOpenJamCors(builder.Configuration);

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

// PathBase：剝除 Ingress 轉發保留的服務前綴（如 /storage-service），須為第一個 middleware
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
