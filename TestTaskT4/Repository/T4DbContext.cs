using Microsoft.EntityFrameworkCore;
using TestTaskT4.Models.Entity;

namespace TestTaskT4.Repository;

public class T4DbContext(DbContextOptions<T4DbContext> options) : DbContext(options)
{
    public DbSet<TransactionEntity> Transactions => Set<TransactionEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TransactionEntity>()
            .HasIndex(t => t.Id)
            .IsUnique();
    }
}