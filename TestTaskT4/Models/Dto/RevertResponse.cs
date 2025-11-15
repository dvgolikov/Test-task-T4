namespace TestTaskT4.Models.Dto;

public record RevertResponse(
    DateTime RevertDateTime,
    decimal ClientBalance);