using LogService.Consumers;
using LogService.Data;
using LogService.Services;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Shared.Auth;
using Shared.Middleware;

var builder = WebApplication.CreateBuilder(args);

// PostgreSQL + EF Core（snake_case 命名慣例）
builder.Services.AddSingleton<ICurrentUserAccessor, WorkerCurrentUserAccessor>();
builder.Services.AddDbContext<LogDbContext>(opts =>
    opts.UseNpgsql(builder.Configuration.GetConnectionString("Postgres"),
            o => o.MigrationsHistoryTable("__ef_migrations_history"))
        .UseSnakeCaseNamingConvention());

// MassTransit + RabbitMQ（consumer + 指數退避重試）
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<AuditLogConsumer>(cfg =>
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

// REST API
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opts =>
{
    opts.SwaggerDoc("v1", new() { Title = "LogService", Version = "v1" });
    var xmlPath = Path.Combine(AppContext.BaseDirectory, "LogService.xml");
    if (File.Exists(xmlPath)) opts.IncludeXmlComments(xmlPath);
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider
        .GetRequiredService<LogDbContext>()
        .Database.MigrateAsync();
}

app.UseExceptionMiddleware();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.Run();
