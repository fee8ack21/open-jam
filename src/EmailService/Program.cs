using EmailService.Consumers;
using EmailService.Data;
using EmailService.Options;
using EmailService.Services;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Shared.Auth;

var builder = Host.CreateApplicationBuilder(args);

// PostgreSQL + EF Core（snake_case 命名慣例）
builder.Services.AddSingleton<ICurrentUserAccessor, WorkerCurrentUserAccessor>();
builder.Services.AddDbContext<EmailDbContext>(opts =>
    opts.UseNpgsql(builder.Configuration.GetConnectionString("Postgres"))
        .UseSnakeCaseNamingConvention());

// SMTP
builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("Smtp"));
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection("Email"));
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();

// MassTransit + RabbitMQ（consumer + 指數退避重試）
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<EmailConsumer>(cfg =>
    {
        // 暫時性失敗（連線逾時等）採指數退避，最多 5 次，1–30 秒區間
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

// 補償重試排程
builder.Services.AddHostedService<EmailRetryService>();

var host = builder.Build();
host.Run();
