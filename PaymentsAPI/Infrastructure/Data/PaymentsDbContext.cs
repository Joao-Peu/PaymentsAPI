using Microsoft.EntityFrameworkCore;
using PaymentsAPI.Core.Entities;

namespace PaymentsAPI.Infrastructure.Data;

public class PaymentsDbContext : DbContext
{
    public PaymentsDbContext(DbContextOptions<PaymentsDbContext> options) : base(options)
    {
    }

    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Ensure default schema is dbo
        modelBuilder.HasDefaultSchema("dbo");

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.ToTable("Payments", "dbo");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.OrderId).IsRequired();
            entity.Property(p => p.Amount).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(p => p.Status).HasConversion<int>().IsRequired();
            entity.Property(p => p.CreatedAt).IsRequired();
            entity.HasIndex(p => p.OrderId).IsUnique();
        });
    }
}
