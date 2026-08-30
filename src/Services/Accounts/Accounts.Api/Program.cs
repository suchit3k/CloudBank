using Accounts.Application.Persistence;
using Accounts.Infrastructure.Persistence;
using MediatR;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);


// Database
var connectionString = builder.Configuration.GetConnectionString("AccountsDb");
builder.Services.AddDbContext<AccountsDbContext>(options =>
    options.UseNpgsql(connectionString));

// Repository
builder.Services.AddScoped<IAccountRepository, AccountRepository>();

// MediatR — scans the Application assembly for all IRequestHandler implementations
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(
        typeof(Accounts.Application.Accounts.Commands.CreateAccount.CreateAccountCommand).Assembly));
builder.Services.AddValidatorsFromAssembly(
    typeof(Accounts.Application.Accounts.Commands.CreateAccount.CreateAccountCommand).Assembly);

builder.Services.AddTransient(
    typeof(MediatR.IPipelineBehavior<,>),
    typeof(Accounts.Application.Behaviors.ValidationBehavior<,>));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

//centralized exception handling,
app.UseMiddleware<Accounts.Api.Middleware.ExceptionHandlingMiddleware>();   

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();