using System.Text.Json.Serialization;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using OrderService.Consumers;
using OrderService.Data;
using OrderService.Options;
using OrderService.Services;
using OrderService.Services.Background;
using OrderService.Services.Orders;
using Shared.Auth;
using Shared.Data;
using Shared.Middleware;
using Shared.Web;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserAccessor, HttpContextUserAccessor>();

builder.Services.AddDbContext<OrderDbContext>(opts =>
    opts.UseOpenJamNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddMassTransit(x =>
{
    // queue 名稱加服務前綴：跨服務同名 consumer（如 UserRegisteredConsumer）才不會綁到同一條
    // queue 變成輪流分食，廣播事件每個服務必須各收一份。
    x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("Order", false));

    x.AddConsumer<PaymentSucceededConsumer>(cfg =>
    {
        cfg.UseMessageRetry(r => r.Exponential(
            retryLimit:    5,
            minInterval:   TimeSpan.FromSeconds(1),
            maxInterval:   TimeSpan.FromSeconds(30),
            intervalDelta: TimeSpan.FromSeconds(2)));
    });

    // 註冊事件回填訪客訂單的 BuyerUserId（冪等 UPDATE）。
    x.AddConsumer<UserRegisteredConsumer>();

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

builder.Services.Configure<ServiceOptions>(builder.Configuration.GetSection("Services"));
builder.Services.Configure<OrderOptions>(builder.Configuration.GetSection("Order"));

// 賣家視角訂單查詢需向 StoreService 驗證商店 Owner 身分（轉發呼叫者 Bearer token）。
var services = builder.Configuration.GetSection("Services").Get<ServiceOptions>() ?? new ServiceOptions();
var storeBaseUrl = (services.StoreService.BaseUrl ?? "http://localhost:5172").TrimEnd('/') + "/";
builder.Services.AddHttpClient("store", client => client.BaseAddress = new Uri(storeBaseUrl));
builder.Services.AddScoped<StoreServiceClient>();

// 結帳建單後向 PaymentService 建立 Stripe Checkout Session（server-to-server，以 service token 認證）。
var paymentBaseUrl = (services.PaymentService.BaseUrl ?? "http://localhost:5178").TrimEnd('/') + "/";
builder.Services.AddHttpClient("payment", client => client.BaseAddress = new Uri(paymentBaseUrl));
builder.Services.AddScoped<PaymentServiceClient>();
builder.Services.AddOpenJamServiceTokenClient(builder.Configuration);

// 結帳核價：向 CatalogService 匿名查詢商品現況（價格 / 版本 / 所屬商店）。
var catalogBaseUrl = (services.CatalogService.BaseUrl ?? "http://localhost:5176").TrimEnd('/') + "/";
builder.Services.AddHttpClient("catalog", client => client.BaseAddress = new Uri(catalogBaseUrl));
builder.Services.AddScoped<CatalogServiceClient>();

builder.Services.AddScoped<IOrderManager, OrderManager>();
builder.Services.AddScoped<AuditLogPublisher>();
builder.Services.AddScoped<OrderEventPublisher>();

builder.Services.AddHostedService<OutboxRelayService>();

builder.Services.AddOpenJamJwtAuth(builder.Configuration);
builder.Services.AddOpenJamCors(builder.Configuration);

builder.Services.AddControllers()
    .AddJsonOptions(opts =>
        opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenJamApiVersioning();
builder.Services.AddOpenJamSwagger("OrderService");

builder.Services.AddOpenJamValidation(typeof(Program).Assembly);
builder.Services.AddOpenJamMapping(typeof(Program).Assembly);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider
        .GetRequiredService<OrderDbContext>()
        .Database.MigrateAsync();
}

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

app.MapGet("/healthz", () => Results.Ok());
app.MapControllers();
app.Run();
