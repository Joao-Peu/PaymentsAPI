using MassTransit;
using PaymentsAPI.Application.Consumers;
using PaymentsAPI.Infrastructure;
using PaymentsAPI.Infrastructure.Repositories;
using PaymentsAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// =======================
// CONFIGURAÇÃO
// =======================
builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables(); // ainda mantém, mas vamos pegar direto do ambiente

// =======================
// CONTROLLERS & SWAGGER
// =======================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// =======================
// SQL SERVER
// =======================
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

// =======================
// RABBITMQ / MASSTRANSIT
// =======================
// Pega direto do ambiente, ignorando appsettings
var rabbitHost = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "rabbitmq";
var rabbitUser = Environment.GetEnvironmentVariable("RABBITMQ_USERNAME") ?? "fiap";
var rabbitPass = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD") ?? "fiap123";
var rabbitVHost = Environment.GetEnvironmentVariable("RABBITMQ_VHOST") ?? "/";
var rabbitQueue = Environment.GetEnvironmentVariable("RABBITMQ_QUEUE_PAYMENT_CREATED") ?? "payment.created";

Console.WriteLine($"Configuring RabbitMQ: Host={rabbitHost}, User={rabbitUser}, VHost={rabbitVHost}, Queue={rabbitQueue}");

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<OrderPlacedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(rabbitHost, rabbitVHost, h =>
        {
            h.Username(rabbitUser);
            h.Password(rabbitPass);
        });

        cfg.ReceiveEndpoint(rabbitQueue, e =>
        {
            e.Durable = true;   // fila durável, aparece no Management UI
            e.AutoDelete = false; // não deleta a fila
            e.ConfigureConsumer<OrderPlacedConsumer>(context);
        });
    });
});

// =======================
// BUILD APP
// =======================
var app = builder.Build();

// =======================
// MIGRATIONS
// =======================
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
    db.Database.Migrate();
}

// =======================
// MIDDLEWARE
// =======================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();
app.Run();
