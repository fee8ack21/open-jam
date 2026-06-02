using Bootstrap.Seeders;
using EmailService.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((ctx, services) =>
    {
        services.AddDbContext<EmailDbContext>(opts =>
            opts.UseNpgsql(ctx.Configuration["ConnectionStrings:Postgres"]));

        var hydraUrl = (ctx.Configuration["Hydra:AdminUrl"] ?? "http://localhost:4445").TrimEnd('/') + "/";
        services.AddHttpClient("hydra", client => client.BaseAddress = new Uri(hydraUrl));

        services.AddScoped<HydraClientSeeder>();
        services.AddScoped<EmailTemplateSeeder>();
    })
    .Build();

using var scope = host.Services.CreateScope();
var sp = scope.ServiceProvider;

await sp.GetRequiredService<HydraClientSeeder>().SeedAsync();
await sp.GetRequiredService<EmailTemplateSeeder>().SeedAsync();
