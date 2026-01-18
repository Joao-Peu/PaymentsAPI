using MassTransit;
using PaymentsAPI.Infrastructure;
using PaymentsAPI.Core.Entities;
using PaymentsAPI.Core.ValueObjects;
using Shared.Events;

namespace PaymentsAPI.Application.Consumers;

public class OrderPlacedConsumer(IPaymentRepository repository, IPublishEndpoint publishEndpoint) : IConsumer<OrderPlacedEvent>
{
    public async Task Consume(ConsumeContext<OrderPlacedEvent> context)
    {
        var message = context.Message;

        var rnd = new Random();
        var approved = rnd.Next(0, 100) < 80;

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = message.OrderId,
            Amount = message.Price,
            Status = approved ? PaymentStatus.Approved : PaymentStatus.Rejected,
            CreatedAt = DateTime.UtcNow
        };

        await repository.AddAsync(payment);

        var processed = new PaymentProcessedEvent(message.OrderId, message.UserId, message.GameId, message.Price, payment.Status.ToString());

        await publishEndpoint.Publish(processed);
    }
}
