namespace TestTaskT4.Models.Dto;

public record BalanceResponse(
    DateTime BalanceDateTime,
    decimal ClientBalance);