using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TestTaskT4.Exceptions;
using TestTaskT4.Models.Dto;
using TestTaskT4.Services;
using TestTaskT4.Transactions;

namespace TestTaskT4.Controllers;

[ApiController]
[Route("[controller]")]
public class TransactionsController(BalanceService balanceService) : ControllerBase
{
    [HttpPost("/credit")]
    public async Task<IActionResult> Credit([FromBody] TransactionRequest request)
    {
        var validateResult = Validate(request);
        return validateResult.Value != null
            ? validateResult
            : Ok(await balanceService.ProcessTransaction(request, TransactionType.Credit));
    }

    [HttpPost("/debit")]
    public async Task<IActionResult> Debit([FromBody] TransactionRequest request)
    {
        var validateResult = Validate(request);

        try
        {
            return validateResult.Value != null
                ? validateResult
                : Ok(await balanceService.ProcessTransaction(request, TransactionType.Debit));
        }
        catch (AppException ex)
        {
            return Problem(title: ex.Title,
                detail: ex.Detail,
                statusCode: ex.StatusCode);
        }
    }

    [HttpPost("/revert")]
    public async Task<IActionResult> Revert([FromQuery] Guid id)
    {
        try
        {
            return Ok(await balanceService.RevertTransaction(id));
        }
        catch (AppException ex)
        {
            return Problem(title: ex.Title,
                detail: ex.Detail,
                statusCode: ex.StatusCode);
        }
    }

    [HttpGet("/balance")]
    public async Task<IActionResult> Balance([FromQuery] Guid id)
    {
        var balance = await balanceService.GetBalance(id);
        return Ok(new BalanceResponse(DateTime.UtcNow, balance));
    }

    private ObjectResult Validate(TransactionRequest request)
    {
        if (request.Amount <= 0)
            return ValidationProblem("amount", "Amount must be positive");

        if (request.DateTime > DateTime.UtcNow)
            return ValidationProblem("dateTime", "Date cannot be in the future");

        return new ObjectResult(null);
    }

    private ObjectResult ValidationProblem(string field, string message)
    {
        return Problem(
            title: "Validation error",
            detail: $"{field}: {message}",
            statusCode: StatusCodes.Status400BadRequest);
    }
}