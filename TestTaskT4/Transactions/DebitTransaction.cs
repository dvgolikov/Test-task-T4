namespace TestTaskT4.Transactions;

public class DebitTransaction
{
    public Guid Id { get; set; }
    public Guid ClientId { get; set; }
    public DateTime DateTime { get; set; }
    public decimal Amount { get; set; }
}