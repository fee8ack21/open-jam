using System.Text.Json.Serialization;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using PaymentService.Data;
using PaymentService.Options;
using PaymentService.Services;
using PaymentService.Services.Background;
using PaymentService.Services.Payments;
using Shared.Auth;
using Shared.Data;
using Shared.Middleware;
using Shared.Web;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserAccessor, HttpContextUserAccessor>();

builder.Services.AddDbContext<PaymentDbContext>(opts =>
    opts.UseOpenJamNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddMassTransit(x =>
{
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

builder.Services.Configure<StripeOptions>(builder.Configuration.GetSection("Stripe"));
builder.Services.Configure<ServiceOptions>(builder.Configuration.GetSection("Services"));

builder.Services.AddScoped<IPaymentManager, PaymentManager>();
builder.Services.AddScoped<StripeWebhookHandler>();
builder.Services.AddScoped<AuditLogPublisher>();

builder.Services.AddHostedService<OutboxRelayService>();

builder.Services.AddOpenJamJwtAuth(builder.Configuration);
builder.Services.AddOpenJamCors(builder.Configuration);

builder.Services.AddControllers()
    .AddJsonOptions(opts =>
        opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenJamApiVersioning();
builder.Services.AddOpenJamSwagger("PaymentService");

builder.Services.AddOpenJamValidation(typeof(Program).Assembly);
builder.Services.AddOpenJamMapping(typeof(Program).Assembly);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider
        .GetRequiredService<PaymentDbContext>()
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
