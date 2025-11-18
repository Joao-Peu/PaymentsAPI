using PaymentsAPI.Core.Entities;

namespace PaymentsAPI.Infrastructure;

public interface IPaymentRepository
{
    Task AddAsync(Payment payment);
    Task<Payment?> GetByOrderIdAsync(Guid orderId);
}
