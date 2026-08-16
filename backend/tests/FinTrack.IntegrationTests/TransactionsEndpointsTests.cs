using System.Net;
using System.Net.Http.Json;
using FinTrack.Application.Common.Models;
using FinTrack.Application.Features.Transactions.Dtos;
using FluentAssertions;

namespace FinTrack.IntegrationTests;

[Collection(ApiCollection.Name)]
public class TransactionsEndpointsTests
{
    private readonly ApiFactory _factory;

    public TransactionsEndpointsTests(ApiFactory factory) => _factory = factory;

    private static object NewExpense(Guid categoryId, decimal amount, string date, string description = "test") => new
    {
        type = "Expense",
        amount,
        categoryId,
        description,
        transactionDate = date
    };

    [Fact]
    public async Task CreateTransaction_WhenValid_ReturnsCreated()
    {
        var client = await _factory.RegisterClientAsync();
        var categoryId = await client.GetCategoryIdAsync("Food");

        var response = await client.PostAsJsonAsync("/api/transactions", NewExpense(categoryId, 450m, "2026-07-12"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<TransactionDto>(ApiTestExtensions.Json);
        created!.Amount.Should().Be(450m);
        created.CategoryName.Should().Be("Food");
    }

    [Fact]
    public async Task CreateTransaction_WhenAmountIsZero_ReturnsValidationError()
    {
        var client = await _factory.RegisterClientAsync();
        var categoryId = await client.GetCategoryIdAsync("Food");

        var response = await client.PostAsJsonAsync("/api/transactions", NewExpense(categoryId, 0m, "2026-07-12"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetTransaction_WhenOwnedByAnotherUser_ReturnsNotFound()
    {
        // User A creates a transaction.
        var clientA = await _factory.RegisterClientAsync();
        var categoryIdA = await clientA.GetCategoryIdAsync("Food");
        var createResponse = await clientA.PostAsJsonAsync("/api/transactions", NewExpense(categoryIdA, 100m, "2026-07-10"));
        var created = await createResponse.Content.ReadFromJsonAsync<TransactionDto>(ApiTestExtensions.Json);

        // User B must not be able to read it.
        var clientB = await _factory.RegisterClientAsync();
        var response = await clientB.GetAsync($"/api/transactions/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetTransactions_SortByAmountAscending_ReturnsInAscendingOrder()
    {
        var client = await _factory.RegisterClientAsync();
        var categoryId = await client.GetCategoryIdAsync("Food");

        await client.PostAsJsonAsync("/api/transactions", NewExpense(categoryId, 300m, "2026-07-03"));
        await client.PostAsJsonAsync("/api/transactions", NewExpense(categoryId, 100m, "2026-07-01"));
        await client.PostAsJsonAsync("/api/transactions", NewExpense(categoryId, 200m, "2026-07-02"));

        var page = await client.GetFromJsonAsync<PagedResult<TransactionDto>>(
            "/api/transactions?sortBy=Amount&sortDir=Asc&pageSize=50", ApiTestExtensions.Json);

        page!.TotalCount.Should().Be(3);
        page.Items.Select(t => t.Amount).Should().ContainInOrder(100m, 200m, 300m);
    }
}
