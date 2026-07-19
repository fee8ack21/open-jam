using Auth.Consumers;
using Auth.Data;
using Auth.Options;
using Auth.Services.Background;
using Auth.Services.Content;
using Auth.Services.Hydra;
using Auth.Services.Legal;
using Auth.Services.Security;
using Auth.Services.Storefront;
using Auth.Services.Users;
using MassTransit;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Shared.Auth;
using Shared.Data;
using Shared.Middleware;
using Shared.Web;
using System.Globalization;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

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
builder.Services.Configure<SecurityOptions>(builder.Configuration.GetSection("Security"));

// 正式環境前置 GCLB（GCE Ingress）：真實客戶端 IP 在 X-Forwarded-For。GCLB 會在既有
// XFF 之後附加「<client>, <lb>」兩段，故 ForwardLimit = 2 取到倒數第二段（真實 client），
// 客戶端自帶的偽造段不會被採信。本機直連無 XFF header 時不作用。
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = 2;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// IP 維度 rate limit（固定視窗，單 pod in-memory；replica > 1 時各 pod 各自計數，屬可接受的放寬）。
// 表單類（登入 / 重置密碼）與寄信類（註冊 / 忘記密碼）分開限額；寄信類另有帳號維度的
// DB 節流（UserService 冷卻 + 每小時上限）作第二道防線。
var security = builder.Configuration.GetSection("Security").Get<SecurityOptions>() ?? new SecurityOptions();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (ctx, ct) =>
    {
        ctx.HttpContext.Response.ContentType = "text/plain; charset=utf-8";
        await ctx.HttpContext.Response.WriteAsync("請求過於頻繁，請稍後再試。", ct);
    };

    static string ClientIp(HttpContext ctx) => ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    options.AddPolicy("auth-form", ctx => RateLimitPartition.GetFixedWindowLimiter(ClientIp(ctx),
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = security.FormRateLimit,
            Window      = TimeSpan.FromSeconds(security.RateLimitWindowSeconds),
            QueueLimit  = 0,
        }));

    options.AddPolicy("auth-email", ctx => RateLimitPartition.GetFixedWindowLimiter(ClientIp(ctx),
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = security.EmailFormRateLimit,
            Window      = TimeSpan.FromSeconds(security.RateLimitWindowSeconds),
            QueueLimit  = 0,
        }));
});

// Cloudflare Turnstile（認證表單人機驗證；金鑰留空即整組停用）。
// 短 timeout：siteverify 失敗採 fail-open，不讓外部服務延遲拖垮表單送出。
builder.Services.Configure<TurnstileOptions>(builder.Configuration.GetSection("Turnstile"));
builder.Services.AddHttpClient("turnstile", client =>
{
    client.BaseAddress = new Uri("https://challenges.cloudflare.com/");
    client.Timeout = TimeSpan.FromSeconds(5);
});
builder.Services.AddScoped<ITurnstileService, TurnstileService>();

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

// MassTransit（Outbox 事件發布 + 消費 StoreProvisionedEvent 註冊店面 redirect URI）
builder.Services.AddMassTransit(x =>
{
    // queue 名稱加服務前綴：跨服務同名 consumer 才不會綁到同一條 queue 變成輪流分食，
    // 廣播事件每個服務必須各收一份。
    x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("Auth", false));

    x.AddConsumer<StoreProvisionedConsumer>(cfg =>
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

// Domain services
builder.Services.AddScoped<IPasswordHasher, Argon2idHasher>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ILegalConsentService, LegalConsentService>();
builder.Services.AddScoped<IStorefrontRedirectService, StorefrontRedirectService>();
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

// 還原真實客戶端 IP（GCLB X-Forwarded-For），rate limit 分區與日誌依賴，須最早執行
app.UseForwardedHeaders();

// 正式環境 REST API 掛在 api.openjam.co/auth-service 之下，GCE Ingress 不剝前綴，
// 由此剝除（設定 PathBase=/auth-service）。不帶前綴的 MVC
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

// IP 維度 rate limit（掛 [EnableRateLimiting] 的認證表單 POST），須在 Routing 之後
app.UseRateLimiter();

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
