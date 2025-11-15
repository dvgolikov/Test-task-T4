using Microsoft.EntityFrameworkCore;
using TestTaskT4.Exceptions;
using TestTaskT4.Models.Dto;
using TestTaskT4.Models.Entity;
using TestTaskT4.Repository;
using TestTaskT4.Transactions;

namespace TestTaskT4.Services;

public class BalanceService(T4DbContext db)
{
    public async Task<decimal> GetBalance(Guid clientId)
    {
        var list = await db.Transactions
            .Where(t => t.ClientId == clientId && !t.Reverted)
            .ToListAsync();

        var credit = list.Where(t => t.Type == TransactionType.Credit).Sum(t => t.Amount);
        var debit = list.Where(t => t.Type == TransactionType.Debit).Sum(t => t.Amount);

        return credit - debit;
    }

    public async Task<TransactionResponse> ProcessTransaction(TransactionRequest request, TransactionType type)
    {
        await db.Database.ExecuteSqlRawAsync("SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;");
        await using var tx = await db.Database.BeginTransactionAsync();

        // идемпотентность: проверка существующей
        var exists = await db.Transactions.FindAsync(request.Id);
        if (exists != null)
        {
            var balance = await GetBalance(request.ClientId);
            return new TransactionResponse(exists.InsertDateTime, balance);
        }

        // проверить баланс при дебете
        if (type == TransactionType.Debit)
        {
            var balance = await GetBalance(request.ClientId);
            if (balance < request.Amount)
                throw new AppException("Insufficient funds", "Client balance is too low", 409);
        }

        var entity = new TransactionEntity
        {
            Id = request.Id,
            ClientId = request.ClientId,
            OperationDateTime = request.DateTime.ToUniversalTime(),
            Amount = request.Amount,
            Type = type,
            InsertDateTime = DateTime.UtcNow,
        };

        db.Transactions.Add(entity);
        await db.SaveChangesAsync();
        await tx.CommitAsync();

        var newBalance = await GetBalance(request.ClientId);
        return new TransactionResponse(entity.InsertDateTime, newBalance);
    }

    public async Task<RevertResponse> RevertTransaction(Guid id)
    {
        await db.Database.ExecuteSqlRawAsync("SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;");
        await using var tx = await db.Database.BeginTransactionAsync();

        var transaction = await db.Transactions.FindAsync(id);
        if (transaction == null)
            throw new AppException("Transaction not found", "Cannot revert missing transaction", 404);

        if (transaction.Reverted)
        {
            return new RevertResponse(DateTime.UtcNow, await GetBalance(transaction.ClientId));
        }

        // При отмене дебета баланс увеличивается, при отмене кредита — уменьшается
        // Проверка, не уйдём ли в минус
        if (transaction.Type == TransactionType.Credit)
        {
            var bal = await GetBalance(transaction.ClientId);
            if (bal < transaction.Amount)
                throw new AppException("Cannot revert", "Reverting the credit will make balance negative", 409);
        }

        transaction.Reverted = true;

        await db.SaveChangesAsync();
        await tx.CommitAsync();

        return new RevertResponse(DateTime.UtcNow, await GetBalance(transaction.ClientId));
    }
}