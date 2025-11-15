using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TestTaskT4.Controllers;
using TestTaskT4.Models.Dto;
using TestTaskT4.Repository;
using TestTaskT4.Services;
using TestTaskT4.Transactions;

namespace TestTaskT4.Tests
{
    public class TransactionsControllerTests
    {
        private T4DbContext CreateInMemoryContext()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            var options = new DbContextOptionsBuilder<T4DbContext>()
                .UseSqlite(connection)
                .Options;

            var ctx = new T4DbContext(options);
            ctx.Database.EnsureCreated();

            return ctx;
        }

        [Fact]
        public async Task Credit_Should_Add_Transaction_And_Increase_Balance()
        {
            var ctx = CreateInMemoryContext();
            var balanceService = new BalanceService(ctx);
            var controller = new TransactionsController(balanceService);

            var req = new TransactionRequest(
                Guid.NewGuid(),
                Guid.NewGuid(),
                DateTime.UtcNow.AddMinutes(-1),
                100m);

            var result = await controller.Credit(req) as OkObjectResult;
            Assert.NotNull(result);
            var resp = result.Value as TransactionResponse;
            Assert.NotNull(resp);
            Assert.Equal(100m, resp.ClientBalance);

            var entity = await ctx.Transactions.FindAsync(req.Id);
            Assert.NotNull(entity);
            Assert.Equal(100m, entity.Amount);
            Assert.Equal(TransactionType.Credit, entity.Type);
        }

        [Fact]
        public async Task Debit_Should_Fail_When_Insufficient_Funds()
        {
            var ctx = CreateInMemoryContext();
            var balanceService = new BalanceService(ctx);
            var controller = new TransactionsController(balanceService);

            var clientId = Guid.NewGuid();

            // Попытка списать при нулевом балансе
            var req = new TransactionRequest(Guid.NewGuid(), clientId, DateTime.UtcNow.AddMinutes(-1), 50m);
            var res = await controller.Debit(req);
            // Ожидаем ProblemResult (409 Conflict в нашем примере)
            Assert.IsType<ObjectResult>(res);
            var obj = res as ObjectResult;
            Assert.Equal(409, obj.StatusCode);
        }

        [Fact]
        public async Task Idempotency_Credit_Should_Return_Same_InsertDateTime_On_Repost()
        {
            var ctx = CreateInMemoryContext();
            var balanceService = new BalanceService(ctx);
            var controller = new TransactionsController(balanceService);

            var id = Guid.NewGuid();
            var clientId = Guid.NewGuid();
            var req = new TransactionRequest(id, clientId, DateTime.UtcNow.AddMinutes(-1), 25m);

            var first = await controller.Credit(req) as OkObjectResult;
            Assert.NotNull(first);
            var firstResp = first.Value as TransactionResponse;
            Assert.NotNull(firstResp);

            // Повторная отправка с тем же Id
            var second = await controller.Credit(req) as OkObjectResult;
            Assert.NotNull(second);
            var secondResp = second.Value as TransactionResponse;
            Assert.NotNull(secondResp);

            Assert.Equal(firstResp.InsertDateTime, secondResp.InsertDateTime);
            Assert.Equal(25m, secondResp.ClientBalance);
        }

        [Fact]
        public async Task Revert_Should_Mark_Reverted_And_Update_Balance()
        {
            var ctx = CreateInMemoryContext();
            var balanceService = new BalanceService(ctx);
            var controller = new TransactionsController(balanceService);

            var clientId = Guid.NewGuid();
            var creditReq = new TransactionRequest(Guid.NewGuid(), clientId, DateTime.UtcNow.AddMinutes(-2), 200m);
            var creditRes = await controller.Credit(creditReq) as OkObjectResult;
            Assert.NotNull(creditRes);

            var debitReq = new TransactionRequest(Guid.NewGuid(), clientId, DateTime.UtcNow.AddMinutes(-1), 50m);
            var debitRes = await controller.Debit(debitReq) as OkObjectResult;
            Assert.NotNull(debitRes);

            // Баланс должен быть 150
            var balResult = await controller.Balance(clientId) as OkObjectResult;
            var balResp = balResult.Value as BalanceResponse;
            Assert.Equal(150m, balResp.ClientBalance);

            // Отменяем дебет
            var revertRes = await controller.Revert(debitReq.Id) as OkObjectResult;
            var revertResp = revertRes.Value as RevertResponse;
            Assert.NotNull(revertResp);
            Assert.Equal(200m, revertResp.ClientBalance);

            // Транзакция должна иметь флаг Reverted
            var debitEntity = await ctx.Transactions.FindAsync(debitReq.Id);
            Assert.True(debitEntity.Reverted);
        }

        [Theory]
        [InlineData(-1, -1)]
        [InlineData(1, 10)]
        public async Task Validation_Should_Reject_Negative_Amount_And_Future_Date(int addDay, decimal amount)
        {
            var ctx = CreateInMemoryContext();
            var balanceService = new BalanceService(ctx);
            var controller = new TransactionsController(balanceService);

            var request = new TransactionRequest(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddMinutes(addDay),
                amount);
            var response = await controller.Credit(request) as ObjectResult;

            Assert.NotNull(response);
            Assert.Equal(400, response.StatusCode);
        }
    }
}