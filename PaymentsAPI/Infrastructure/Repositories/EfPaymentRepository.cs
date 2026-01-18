using Microsoft.EntityFrameworkCore;
using PaymentsAPI.Core.Entities;
using PaymentsAPI.Infrastructure.Data;

namespace PaymentsAPI.Infrastructure.Repositories;

public class EfPaymentRepository : IPaymentRepository
{
    private readonly PaymentsDbContext _db;

    public EfPaymentRepository(PaymentsDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(Payment payment)
    {
        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();
    }

    public Task<Payment?> GetByOrderIdAsync(Guid orderId)
    {
        return _db.Payments.AsNoTracking().FirstOrDefaultAsync(p => p.OrderId == orderId);
    }
}
