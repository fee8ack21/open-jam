using Auth.Services.Security;
using Bootstrap.Seeders;
using EmailService.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shared.Auth;
using AuthDbContext = Auth.Data.AppDbContext;
using EmailDbContext = EmailService.Data.AppDbContext;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((ctx, services) =>
    {
        services.AddScoped<ICurrentUserAccessor, NullCurrentUserAccessor>();

        services.AddDbContext<EmailDbContext>(opts =>
            opts.UseNpgsql(ctx.Configuration["ConnectionStrings:EmailConnection"],
                    o => o.MigrationsHistoryTable("__ef_migrations_history"))
                .UseSnakeCaseNamingConvention());

        services.AddDbContext<AuthDbContext>(opts =>
            opts.UseNpgsql(ctx.Configuration["ConnectionStrings:AuthConnection"],
                    o => o.MigrationsHistoryTable("__ef_migrations_history"))
                .UseSnakeCaseNamingConvention());

        services.AddScoped<IPasswordHasher, Argon2idHasher>();

        var hydraUrl = (ctx.Configuration["Hydra:AdminUrl"] ?? "http://localhost:4445").TrimEnd('/') + "/";
        services.AddHttpClient("hydra", client => client.BaseAddress = new Uri(hydraUrl));

        services.AddScoped<HydraClientSeeder>();
        services.AddScoped<EmailTemplateSeeder>();
        services.AddScoped<UserSeeder>();
    })
    .Build();

using var scope = host.Services.CreateScope();
var sp = scope.ServiceProvider;

await sp.GetRequiredService<HydraClientSeeder>().SeedAsync();
await sp.GetRequiredService<EmailTemplateSeeder>().SeedAsync();
await sp.GetRequiredService<UserSeeder>().SeedAsync();

// TODO: SubdomainReservedWordSeeder — 待 Auth 或 Product DbContext 建立後接入。
// 負責將系統占用子網域（auth / workspace / creator / market / api / www / mail）
// 與一般保留字（admin / support / help / blog / static / cdn / …）寫入對應服務 DB。
