using PaymentsAPI.Core.Entities;
using PaymentsAPI.Core.ValueObjects;

namespace PaymentsAPI.Infrastructure.Repositories;

public class InMemoryPaymentRepository : IPaymentRepository
{
    private readonly List<Payment> _store = new();

    public Task AddAsync(Payment payment)
    {
        _store.Add(payment);
        return Task.CompletedTask;
    }

    public Task<Payment?> GetByOrderIdAsync(Guid orderId)
    {
        return Task.FromResult(_store.FirstOrDefault(p => p.OrderId == orderId));
    }
}
