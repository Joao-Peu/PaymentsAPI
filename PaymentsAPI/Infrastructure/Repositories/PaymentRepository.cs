using Microsoft.EntityFrameworkCore;
using PaymentsAPI.Core.Entities;
using PaymentsAPI.Infrastructure.Data;

namespace PaymentsAPI.Infrastructure.Repositories;

public class PaymentRepository(PaymentsDbContext db) : IPaymentRepository
{
    public async Task AddAsync(Payment payment)
    {
        db.Payments.Add(payment);
        await db.SaveChangesAsync();
    }

    public Task<Payment?> GetByOrderIdAsync(Guid orderId)
    {
        return db.Payments.AsNoTracking().FirstOrDefaultAsync(p => p.OrderId == orderId);
    }
}
