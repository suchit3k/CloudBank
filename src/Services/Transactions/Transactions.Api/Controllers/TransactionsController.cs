using MediatR;
using Microsoft.AspNetCore.Mvc;
using Transactions.Application.Transactions.Commands.TransferMoney;
using Transactions.Application.Transactions.Queries.GetTransactionById;

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

    [HttpGet("{id}")]
    public async Task<IActionResult> GetTransaction(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetTransactionByIdQuery(id);
        var transaction = await _mediator.Send(query, cancellationToken);
        return Ok(transaction);
    }
}

public record TransferMoneyRequest(
    Guid SenderAccountId,
    Guid ReceiverAccountId,
    decimal Amount,
    string Currency,
    Guid IdempotencyKey);