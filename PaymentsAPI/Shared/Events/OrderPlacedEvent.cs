namespace Shared.Events;

public record OrderPlacedEvent(Guid OrderId, Guid UserId, Guid GameId, decimal Price);