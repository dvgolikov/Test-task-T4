namespace TestTaskT4.Models.Dto;

public record TransactionRequest(
    Guid Id,
    Guid ClientId,
    DateTime DateTime,
    decimal Amount);