using Transactions.Application.Persistence;
using Transactions.Application.Services;
using Transactions.Infrastructure.Persistence;
using Transactions.Infrastructure.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Database
var connectionString = builder.Configuration.GetConnectionString("TransactionsDb");
builder.Services.AddDbContext<TransactionDbContext>(options =>
    options.UseNpgsql(connectionString));

// Repository
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();

// HTTP client for calling Accounts service
builder.Services.AddHttpClient("AccountsService", client =>
{
    var accountsBaseUrl = builder.Configuration["Services:AccountsService:BaseUrl"];
    client.BaseAddress = new Uri(accountsBaseUrl!);
});
builder.Services.AddScoped<IAccountsServiceClient, AccountsServiceClient>();

// MediatR
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(
        typeof(Transactions.Application.Transactions.Commands.TransferMoney.TransferMoneyCommand).Assembly));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();