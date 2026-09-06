using Accounts.Application.Accounts.Commands.CreateAccount;
using Accounts.Application.Accounts.Commands.Deposit;
using Accounts.Application.Accounts.Commands.Withdraw;
using Accounts.Application.Accounts.Queries.GetAccountById;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Accounts.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AccountsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> CreateAccount(
        [FromBody] CreateAccountRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateAccountCommand(request.UserId, request.AccountType, request.Currency);
        var accountId = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(nameof(GetAccount), new { id = accountId }, new { id = accountId });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetAccount(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetAccountByIdQuery(id);
        var account = await _mediator.Send(query, cancellationToken);

        return Ok(account);
    }

    [HttpPost("{id}/deposit")]
    public async Task<IActionResult> Deposit(Guid id, [FromBody] DepositRequest request, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DepositCommand(id, request.Amount), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id}/withdraw")]
    public async Task<IActionResult> Withdraw(Guid id, [FromBody] WithdrawRequest request, CancellationToken cancellationToken)
    {
        await _mediator.Send(new WithdrawCommand(id, request.Amount), cancellationToken);
        return NoContent();
    }
}

public record CreateAccountRequest(Guid UserId, string AccountType, string Currency = "USD");
public record DepositRequest(decimal Amount);
public record WithdrawRequest(decimal Amount);