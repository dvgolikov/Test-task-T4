using TestTaskT4.Abstractions;

namespace TestTaskT4.Transactions;

public class CreditTransaction : ITransaction
{
    public Guid Id { get; set; }
    public Guid ClientId { get; set; }
    public DateTime DateTime { get; set; }
    public decimal Amount { get; set; }
}