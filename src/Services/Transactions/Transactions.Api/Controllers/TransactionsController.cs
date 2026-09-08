using Transactions.Application.Transactions.Commands.TransferMoney;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Transactions.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TransactionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public TransactionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("transfer")]
    public async Task<IActionResult> Transfer(
        [FromBody] TransferMoneyRequest request,
        CancellationToken cancellationToken)
    {
        var command = new TransferMoneyCommand(
            request.SenderAccountId,
            request.ReceiverAccountId,
            request.Amount,
            request.Currency,
            request.IdempotencyKey);

        var transactionId = await _mediator.Send(command, cancellationToken);

        return Ok(new { transactionId });
    }
}

public record TransferMoneyRequest(
    Guid SenderAccountId,
    Guid ReceiverAccountId,
    decimal Amount,
    string Currency,
    Guid IdempotencyKey);