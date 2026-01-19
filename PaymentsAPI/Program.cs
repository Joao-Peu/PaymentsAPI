using MassTransit;
using PaymentsAPI.Application.Consumers;
using PaymentsAPI.Infrastructure;
using PaymentsAPI.Infrastructure.Repositories;
using PaymentsAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using UsersAPI.Infrastructure.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);

// Configuration
builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// Services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// EF Core - SQL Server
var connectionString = builder.Configuration.GetConnectionString("PaymentsDb");

builder.Services.AddDbContext<PaymentsDbContext>(options =>
    options.UseSqlServer(connectionString, sql =>
    {
        sql.MigrationsAssembly(Assembly.GetExecutingAssembly().GetName().Name);
        // Enable connection resiliency for transient failures
        sql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(5), errorNumbersToAdd: null);
    }));

builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();

var rabbitMQSettings = builder.Configuration.GetSection("RabbitMQ").Get<RabbitMQSettings>()!;
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<OrderPlacedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(rabbitMQSettings.HostName, h =>
        {
            h.Username(rabbitMQSettings.UserName);
            h.Password(rabbitMQSettings.Password);
        });

        cfg.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

// Apply EF Core migrations on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
    db.Database.Migrate();
}

// Configure middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

app.Run();
