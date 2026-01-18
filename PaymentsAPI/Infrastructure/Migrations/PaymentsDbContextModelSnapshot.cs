using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PaymentsAPI.Infrastructure.Data;

#nullable disable

namespace PaymentsAPI.Infrastructure.Migrations
{
    [DbContext(typeof(PaymentsDbContext))]
    partial class PaymentsDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.6");
            modelBuilder.HasAnnotation("Relational:MaxIdentifierLength", 128);
            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("PaymentsAPI.Core.Entities.Payment", b =>
            {
                b.Property<Guid>("Id")
                    .HasColumnType("uniqueidentifier");

                b.Property<decimal>("Amount")
                    .HasColumnType("decimal(18,2)");

                b.Property<DateTime>("CreatedAt")
                    .HasColumnType("datetime2");

                b.Property<Guid>("OrderId")
                    .HasColumnType("uniqueidentifier");

                b.Property<int>("Status")
                    .HasColumnType("int");

                b.HasKey("Id");

                b.HasIndex("OrderId")
                    .IsUnique();

                b.ToTable("Payments", (string)null);
            });
#pragma warning restore 612, 618
        }
    }
}
