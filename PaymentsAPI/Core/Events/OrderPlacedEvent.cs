namespace PaymentsAPI.Core.Events;

public record OrderPlacedEvent(Guid OrderId, Guid UserId, Guid GameId, decimal Price);
