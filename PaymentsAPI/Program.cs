using MassTransit;
using PaymentsAPI.Application.Consumers;
using PaymentsAPI.Infrastructure;
using PaymentsAPI.Infrastructure.Repositories;
using PaymentsAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using UsersAPI.Infrastructure.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables(); 

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString =
    builder.Configuration.GetConnectionString("PaymentsDb")
    ?? Environment.GetEnvironmentVariable("ConnectionStrings__PaymentsDb");

if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("Connection string PaymentsDb not configured");

builder.Services.AddDbContext<PaymentsDbContext>(options =>
    options.UseSqlServer(connectionString, sql =>
    {
        sql.MigrationsAssembly(Assembly.GetExecutingAssembly().GetName().Name);
        sql.EnableRetryOnFailure();
    }));

builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();

builder.Services.AddMassTransit(x =>
{
    var rabbitMQSettings = builder.Configuration.GetSection("RabbitMQ").Get<RabbitMQSettings>()!;
    x.AddConsumer<OrderPlacedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(rabbitMQSettings.HostName, "/", h =>
        {
            h.Username(rabbitMQSettings.UserName);
            h.Password(rabbitMQSettings.Password);
        });

        cfg.ReceiveEndpoint("payments-order-placed", e =>
        {
            e.ConfigureConsumer<OrderPlacedConsumer>(context);
        });
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => true
});

app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false
});
app.UseAuthorization();
app.MapControllers();
app.Run();
