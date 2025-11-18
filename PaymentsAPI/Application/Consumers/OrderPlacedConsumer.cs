using MassTransit;
using PaymentsAPI.Core.Events;
using PaymentsAPI.Infrastructure;
using PaymentsAPI.Core.Entities;
using PaymentsAPI.Core.ValueObjects;

namespace PaymentsAPI.Application.Consumers;

public class OrderPlacedConsumer : IConsumer<OrderPlacedEvent>
{
    private readonly IPaymentRepository _repository;
    private readonly IPublishEndpoint _publishEndpoint;

    public OrderPlacedConsumer(IPaymentRepository repository, IPublishEndpoint publishEndpoint)
    {
        _repository = repository;
        _publishEndpoint = publishEndpoint;
    }

    public async Task Consume(ConsumeContext<OrderPlacedEvent> context)
    {
        var message = context.Message;

        // Simulate payment processing
        var rnd = new Random();
        var approved = rnd.Next(0, 100) < 80; // 80% approval rate

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = message.OrderId,
            Amount = message.Price,
            Status = approved ? PaymentStatus.Approved : PaymentStatus.Rejected,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(payment);

        var processed = new PaymentProcessedEvent(message.OrderId, message.UserId, message.GameId, message.Price, payment.Status.ToString());

        await _publishEndpoint.Publish(processed);
    }
}
