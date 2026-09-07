using System.Net;
using System.Net.Http.Json;
using Transactions.Application.Services;

namespace Transactions.Infrastructure.Services;

public class AccountsServiceClient : IAccountsServiceClient
{
    private readonly HttpClient _httpClient;

    public AccountsServiceClient(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("AccountsService");
    }

    public async Task WithdrawAsync(Guid accountId, decimal amount, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"api/accounts/{accountId}/withdraw",
            new { Amount = amount },
            cancellationToken);

        await EnsureSuccessOrThrow(response, "withdraw");
    }

    public async Task DepositAsync(Guid accountId, decimal amount, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"api/accounts/{accountId}/deposit",
            new { Amount = amount },
            cancellationToken);

        await EnsureSuccessOrThrow(response, "deposit");
    }

    private static async Task EnsureSuccessOrThrow(HttpResponseMessage response, string operation)
    {
        if (response.IsSuccessStatusCode)
            return;

        var errorBody = await response.Content.ReadAsStringAsync();

        if (response.StatusCode == HttpStatusCode.NotFound)
            throw new KeyNotFoundException($"Account not found during {operation}: {errorBody}");

        if (response.StatusCode == HttpStatusCode.BadRequest)
            throw new InvalidOperationException($"Accounts service rejected {operation}: {errorBody}");

        throw new HttpRequestException($"Unexpected error calling Accounts service during {operation}: {response.StatusCode} - {errorBody}");
    }
}