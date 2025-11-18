namespace PaymentsAPI.Core.Events;

public record PaymentProcessedEvent(Guid OrderId, Guid UserId, Guid GameId, decimal Price, string Status);
