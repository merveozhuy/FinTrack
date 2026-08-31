using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using FinTrack.Application.Features.CreditCards.Dtos;
using FluentAssertions;

namespace FinTrack.IntegrationTests;

[Collection(ApiCollection.Name)]
public class CreditCardEndpointsTests
{
    private readonly ApiFactory _factory;

    public CreditCardEndpointsTests(ApiFactory factory) => _factory = factory;

    private static string Today() =>
        DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static async Task<Guid> CreateCardAsync(HttpClient client, string name, decimal? limit = null)
    {
        var response = await client.PostAsJsonAsync("/api/credit-cards", new { name, last4 = "1234", creditLimit = limit });
        response.EnsureSuccessStatusCode();
        var card = await response.Content.ReadFromJsonAsync<CreditCardDto>(ApiTestExtensions.Json);
        return card!.Id;
    }

    private static Task<HttpResponseMessage> AddExpenseOnCardAsync(HttpClient client, Guid categoryId, Guid? cardId, decimal amount) =>
        client.PostAsJsonAsync("/api/transactions", new
        {
            type = "Expense",
            amount,
            categoryId,
            creditCardId = cardId,
            transactionDate = Today(),
        });

    [Fact]
    public async Task ExpenseOnCard_AddsToCardDebt()
    {
        var client = await _factory.RegisterClientAsync();
        var foodId = await client.GetCategoryIdAsync("Food");
        var cardId = await CreateCardAsync(client, "Bonus", 5000m);

        (await AddExpenseOnCardAsync(client, foodId, cardId, 750m)).EnsureSuccessStatusCode();

        var cards = await client.GetFromJsonAsync<List<CreditCardDto>>("/api/credit-cards", ApiTestExtensions.Json);
        var card = cards!.Single(c => c.Id == cardId);
        card.CurrentDebt.Should().Be(750m);
        card.AvailableLimit.Should().Be(4250m);
    }

    [Fact]
    public async Task Payment_ReducesCardDebt()
    {
        var client = await _factory.RegisterClientAsync();
        var foodId = await client.GetCategoryIdAsync("Food");
        var cardId = await CreateCardAsync(client, "Maximum");
        (await AddExpenseOnCardAsync(client, foodId, cardId, 1000m)).EnsureSuccessStatusCode();

        var response = await client.PostAsJsonAsync($"/api/credit-cards/{cardId}/payments",
            new { amount = 400m, paymentDate = Today() });
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var card = await response.Content.ReadFromJsonAsync<CreditCardDto>(ApiTestExtensions.Json);
        card!.CurrentDebt.Should().Be(600m);
    }

    [Fact]
    public async Task CreateExpense_WithAnotherUsersCard_ReturnsBadRequest()
    {
        var clientA = await _factory.RegisterClientAsync();
        var cardId = await CreateCardAsync(clientA, "Axess");

        var clientB = await _factory.RegisterClientAsync();
        var foodB = await clientB.GetCategoryIdAsync("Food");

        var response = await AddExpenseOnCardAsync(clientB, foodB, cardId, 100m);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddPayment_ToAnotherUsersCard_ReturnsNotFound()
    {
        var clientA = await _factory.RegisterClientAsync();
        var cardId = await CreateCardAsync(clientA, "World");

        var clientB = await _factory.RegisterClientAsync();
        var response = await clientB.PostAsJsonAsync($"/api/credit-cards/{cardId}/payments",
            new { amount = 50m, paymentDate = Today() });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
