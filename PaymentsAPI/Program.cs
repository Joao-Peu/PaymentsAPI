using MassTransit;
using PaymentsAPI.Application.Consumers;
using PaymentsAPI.Infrastructure;
using PaymentsAPI.Infrastructure.Repositories;
using PaymentsAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Configuration
builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// Controllers & Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// =======================
// SQL SERVER
// =======================
var connectionString =
    builder.Configuration.GetConnectionString("PaymentsDb")
    ?? builder.Configuration["ConnectionStrings__PaymentsDb"];

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
// RABBITMQ
// =======================
// Usar variáveis sem __, correspondendo ao ConfigMap/Secret
var rabbitHost = builder.Configuration["RABBITMQ_HOST"];
var rabbitUser = builder.Configuration["RABBITMQ_USERNAME"];
var rabbitPass = builder.Configuration["RABBITMQ_PASSWORD"];

if (string.IsNullOrWhiteSpace(rabbitHost))
    throw new InvalidOperationException("RabbitMQ host not configured");

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<OrderPlacedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(rabbitHost, "/", h =>
        {
            h.Username(rabbitUser);
            h.Password(rabbitPass);
        });

        // Configura o endpoint da fila PaymentsAPI
        cfg.ReceiveEndpoint(builder.Configuration["RABBITMQ_QUEUE_PAYMENT_CREATED"], e =>
        {
            e.ConfigureConsumer<OrderPlacedConsumer>(context);
        });
    });
});

var app = builder.Build();

// =======================
// MIGRATIONS
// =======================
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
    db.Database.Migrate();
}

// Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();
app.Run();
