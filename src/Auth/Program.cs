using Auth.Data;
using Auth.Options;
using Auth.Services;
using Auth.Services.Hydra;
using Auth.Services.Security;
using Auth.Services.Users;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Shared.Auth;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// Hydra Admin API client
var hydraAdminUrl = (builder.Configuration["Hydra:AdminUrl"] ?? "http://localhost:4445").TrimEnd('/') + "/";
builder.Services.AddHttpClient("hydra", client => client.BaseAddress = new Uri(hydraAdminUrl));
builder.Services.AddScoped<IHydraService, HydraService>();

// App options
builder.Services.Configure<AppOptions>(builder.Configuration.GetSection("App"));

// HttpContext accessor（ICurrentUserAccessor 依賴）
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserAccessor, HttpContextUserAccessor>();

// PostgreSQL + EF Core（snake_case 命名慣例）
builder.Services.AddDbContext<AppDbContext>(opts =>
    opts.UseNpgsql(builder.Configuration.GetConnectionString("Postgres"),
            o => o.MigrationsHistoryTable("__ef_migrations_history"))
        .UseSnakeCaseNamingConvention());

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
builder.Services.AddHostedService<OutboxRelayService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider
        .GetRequiredService<AppDbContext>()
        .Database.MigrateAsync();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

// K8s liveness/readiness probe 用，固定回 200，避免 MVC `/` 302 導頁誤判為 unhealthy
app.MapGet("/healthz", () => Results.Ok());

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
