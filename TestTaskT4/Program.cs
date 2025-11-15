using Microsoft.EntityFrameworkCore;
using TestTaskT4.Repository;
using TestTaskT4.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<T4DbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddScoped<BalanceService>();

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "TestTask Transactions API",
        Version = "v1",
        Description = "API for credits, debits, revert and balance"
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<T4DbContext>();
    dbContext.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "TestTask Transactions API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseRouting();

app.MapControllers();

app.Run();