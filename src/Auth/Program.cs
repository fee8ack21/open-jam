using Auth.Data;
using Auth.Options;
using Auth.Services.Background;
using Auth.Services.Content;
using Auth.Services.Hydra;
using Auth.Services.Legal;
using Auth.Services.Security;
using Auth.Services.Users;
using MassTransit;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Shared.Auth;
using Shared.Data;
using Shared.Middleware;
using Shared.Web;
using System.Globalization;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// MVC（Razor 視圖）+ REST API。
// enum 一律以名稱（字串）序列化，讓 OpenAPI 產出具名 enum，前端 codegen 不再是 Value0..N。
builder.Services.AddControllersWithViews()
    .AddJsonOptions(opts =>
        opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// REST API（/v1 管理 API）：版本化 + Swagger + FluentValidation + AutoMapper
builder.Services.AddOpenJamApiVersioning();
builder.Services.AddOpenJamSwagger("Auth");
builder.Services.AddOpenJamValidation(typeof(Program).Assembly);
builder.Services.AddOpenJamMapping(typeof(Program).Assembly);

// JWT Bearer 驗證（驗 Hydra JWKS）+ "Admin" 授權 policy，保護 /v1 管理 API
builder.Services.AddOpenJamJwtAuth(builder.Configuration);

// CORS（允許前端 SPA 跨來源呼叫 REST API）
builder.Services.AddOpenJamCors(builder.Configuration);

// Localization
builder.Services.AddLocalization();

var supportedCultures = new[] { new CultureInfo("zh-Hant"), new CultureInfo("en") };
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture("zh-Hant");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
    options.RequestCultureProviders.Clear();
    options.RequestCultureProviders.Add(new QueryStringRequestCultureProvider());
    options.RequestCultureProviders.Add(new CookieRequestCultureProvider());
    options.RequestCultureProviders.Add(new AcceptLanguageHeaderRequestCultureProvider());
});

// Hydra Admin API client
var hydraAdminUrl = (builder.Configuration["Hydra:AdminUrl"] ?? "http://localhost:4445").TrimEnd('/') + "/";
builder.Services.AddHttpClient("hydra", client => client.BaseAddress = new Uri(hydraAdminUrl));
builder.Services.AddScoped<IHydraService, HydraService>();

// App options
builder.Services.Configure<AppOptions>(builder.Configuration.GetSection("App"));
builder.Services.Configure<ServiceOptions>(builder.Configuration.GetSection("Services"));

// ContentService API client（取得啟用中法律文件供註冊 / re-consent 同意流程）
var contentBaseUrl = (builder.Configuration["Services:ContentService:BaseUrl"] ?? "http://localhost:5181").TrimEnd('/') + "/";
builder.Services.AddHttpClient("content", client => client.BaseAddress = new Uri(contentBaseUrl));
builder.Services.AddScoped<ContentServiceClient>();

// HttpContext accessor（ICurrentUserAccessor 依賴）
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserAccessor, HttpContextUserAccessor>();

// PostgreSQL + EF Core（snake_case 命名慣例）
builder.Services.AddDbContext<AppDbContext>(opts =>
    opts.UseOpenJamNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// MassTransit (publisher only)
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

// Domain services
builder.Services.AddScoped<IPasswordHasher, Argon2idHasher>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ILegalConsentService, LegalConsentService>();
builder.Services.AddHostedService<OutboxRelayService>();

var app = builder.Build();

// 產生 OpenAPI 文件（dotnet swagger tofile）等離線工具情境下，略過需連線外部資源的啟動程序。
var skipStartupTasks = Environment.GetEnvironmentVariable("OPENJAM_SKIP_STARTUP_TASKS") == "true";

if (!skipStartupTasks)
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider
        .GetRequiredService<AppDbContext>()
        .Database.MigrateAsync();
}

// 正式環境 REST API 掛在 api.openjam.co/auth-service 之下，GCE Ingress 不剝前綴，
// 由此剝除（設定 PathBase=/auth-service）。須為第一個 middleware；不帶前綴的 MVC
// 頁面（auth.openjam.co/login…）與 /healthz 不符前綴，原樣通過、不受影響。
app.UseOpenJamPathBase();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// REST API（/v1）例外統一轉 RFC 9457 Problem Details；
// MVC 視圖頁面不在此分支，仍由 UseExceptionHandler 導向 Error 頁。
app.UseWhen(
    ctx => ctx.Request.Path.StartsWithSegments("/v1"),
    api => api.UseExceptionMiddleware());

app.UseRequestLocalization();

app.UseOpenJamCors();

app.UseAuthentication();
app.UseAuthorization();

// K8s liveness/readiness probe 用，固定回 200，避免 MVC `/` 302 導頁誤判為 unhealthy
app.MapGet("/healthz", () => Results.Ok()).ExcludeFromDescription();

// 屬性路由的 REST API 控制器（/v1/users…）
app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
