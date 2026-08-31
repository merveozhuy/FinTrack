using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using FinTrack.Application.Features.RecurringTransactions.Dtos;
using FinTrack.Application.Features.RecurringTransactions.Processing;
using FinTrack.Application.Common.Models;
using FinTrack.Application.Features.Transactions.Dtos;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace FinTrack.IntegrationTests;

[Collection(ApiCollection.Name)]
public class RecurringTransactionsEndpointsTests
{
    private readonly ApiFactory _factory;

    public RecurringTransactionsEndpointsTests(ApiFactory factory) => _factory = factory;

    private static object Rule(Guid categoryId, string startDate, string frequency = "Monthly") => new
    {
        type = "Expense",
        amount = 100m,
        currency = "TRY",
        categoryId,
        description = "subscription",
        frequency,
        startDate,
        endDate = (string?)null
    };

    private static string Iso(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    [Fact]
    public async Task Create_SchedulesFirstOccurrenceOnStartDate()
    {
        var client = await _factory.RegisterClientAsync();
        var foodId = await client.GetCategoryIdAsync("Food");

        var response = await client.PostAsJsonAsync("/api/recurring-transactions", Rule(foodId, "2030-01-01"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<RecurringTransactionDto>(ApiTestExtensions.Json);
        created!.NextExecutionDate.Should().Be(new DateOnly(2030, 1, 1));
        created.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateStatus_CanPauseARule()
    {
        var client = await _factory.RegisterClientAsync();
        var foodId = await client.GetCategoryIdAsync("Food");
        var created = await (await client.PostAsJsonAsync("/api/recurring-transactions", Rule(foodId, "2030-01-01")))
            .Content.ReadFromJsonAsync<RecurringTransactionDto>(ApiTestExtensions.Json);

        var response = await client.PatchAsJsonAsync(
            $"/api/recurring-transactions/{created!.Id}/status", new { isActive = false });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<RecurringTransactionDto>(ApiTestExtensions.Json);
        updated!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateStatus_WhenOwnedByAnotherUser_ReturnsNotFound()
    {
        var clientA = await _factory.RegisterClientAsync();
        var foodId = await clientA.GetCategoryIdAsync("Food");
        var created = await (await clientA.PostAsJsonAsync("/api/recurring-transactions", Rule(foodId, "2030-01-01")))
            .Content.ReadFromJsonAsync<RecurringTransactionDto>(ApiTestExtensions.Json);

        var clientB = await _factory.RegisterClientAsync();
        var response = await clientB.PatchAsJsonAsync(
            $"/api/recurring-transactions/{created!.Id}/status", new { isActive = false });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ProcessDue_GeneratesTransaction_AndIsIdempotent()
    {
        var client = await _factory.RegisterClientAsync();
        var foodId = await client.GetCategoryIdAsync("Food");
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        await client.PostAsJsonAsync("/api/recurring-transactions", Rule(foodId, Iso(today)));

        await ProcessDueAsync(today);
        var afterFirst = await client.GetFromJsonAsync<PagedResult<TransactionDto>>(
            "/api/transactions", ApiTestExtensions.Json);

        // Running again the same day must not create a duplicate (idempotency via NextExecutionDate).
        await ProcessDueAsync(today);
        var afterSecond = await client.GetFromJsonAsync<PagedResult<TransactionDto>>(
            "/api/transactions", ApiTestExtensions.Json);

        afterFirst!.TotalCount.Should().Be(1);
        afterFirst.Items[0].Amount.Should().Be(100m);
        afterFirst.Items[0].TransactionDate.Should().Be(today);
        afterSecond!.TotalCount.Should().Be(1);
    }

    private async Task ProcessDueAsync(DateOnly today)
    {
        using var scope = _factory.Services.CreateScope();
        var processor = scope.ServiceProvider.GetRequiredService<IRecurringTransactionProcessor>();
        await processor.ProcessDueAsync(today, CancellationToken.None);
    }
}
