using TestTaskT4.Transactions;

namespace TestTaskT4.Models.Entity;

public class TransactionEntity
{
    public Guid Id { get; set; }
    public Guid ClientId { get; set; }
    public DateTime OperationDateTime { get; set; }
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public bool Reverted { get; set; }
    public DateTime InsertDateTime { get; set; }
}