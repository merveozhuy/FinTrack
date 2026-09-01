using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using FinTrack.Application.Features.Assistant.Dtos;
using FluentAssertions;

namespace FinTrack.IntegrationTests;

[Collection(ApiCollection.Name)]
public class AssistantEndpointsTests
{
    private readonly ApiFactory _factory;

    public AssistantEndpointsTests(ApiFactory factory) => _factory = factory;

    private static string ThisMonth(int day) =>
        new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, day).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static async Task AddExpenseAsync(HttpClient client, Guid categoryId, decimal amount)
    {
        var response = await client.PostAsJsonAsync("/api/transactions", new
        {
            type = "Expense",
            amount,
            categoryId,
            description = "test",
            transactionDate = ThisMonth(10),
        });
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Chat_WithData_ReturnsGroundedAnswerAndPersistsConversation()
    {
        var client = await _factory.RegisterClientAsync();
        var foodId = await client.GetCategoryIdAsync("Food");
        await AddExpenseAsync(client, foodId, 320m);

        var chat = await client.PostAsJsonAsync("/api/assistant/chat", new { message = "How much did I spend this month?" });
        chat.StatusCode.Should().Be(HttpStatusCode.OK);
        var response = await chat.Content.ReadFromJsonAsync<ChatResponse>(ApiTestExtensions.Json);

        response!.Answer.Should().NotBeNullOrWhiteSpace();
        response.Sources.Should().NotBeEmpty();

        var conversations = await client.GetFromJsonAsync<List<ConversationSummaryDto>>(
            "/api/assistant/conversations", ApiTestExtensions.Json);
        conversations.Should().ContainSingle(c => c.Id == response.ConversationId);

        var detail = await client.GetFromJsonAsync<ConversationDetailDto>(
            $"/api/assistant/conversations/{response.ConversationId}", ApiTestExtensions.Json);
        detail!.Messages.Should().HaveCount(2); // the user message and the assistant reply
    }

    [Fact]
    public async Task Chat_WithNoData_SaysNotEnoughData()
    {
        var client = await _factory.RegisterClientAsync();

        var chat = await client.PostAsJsonAsync("/api/assistant/chat", new { message = "How much did I spend?" });
        var response = await chat.Content.ReadFromJsonAsync<ChatResponse>(ApiTestExtensions.Json);

        response!.Answer.Should().Contain("yeterli veri");
    }

    [Fact]
    public async Task Chat_LastMonthQuestion_UsesLastMonthPeriod()
    {
        var client = await _factory.RegisterClientAsync();
        var foodId = await client.GetCategoryIdAsync("Food");

        var lastMonth = DateTime.UtcNow.AddMonths(-1);
        var date = new DateOnly(lastMonth.Year, lastMonth.Month, 10).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        await client.PostAsJsonAsync("/api/transactions",
            new { type = "Expense", amount = 640m, categoryId = foodId, transactionDate = date });

        var chat = await client.PostAsJsonAsync("/api/assistant/chat", new { message = "Geçen ay ne kadar harcadım?" });
        var response = await chat.Content.ReadFromJsonAsync<ChatResponse>(ApiTestExtensions.Json);

        response!.DataPeriod.Start.Should().Be(new DateOnly(lastMonth.Year, lastMonth.Month, 1));
        response.Answer.Should().Contain("640");
    }

    [Fact]
    public async Task Chat_AboutCardDebt_IncludesDebtInAnswer()
    {
        var client = await _factory.RegisterClientAsync();
        var foodId = await client.GetCategoryIdAsync("Food");
        var card = await (await client.PostAsJsonAsync("/api/credit-cards", new { name = "Bonus" }))
            .Content.ReadFromJsonAsync<FinTrack.Application.Features.CreditCards.Dtos.CreditCardDto>(ApiTestExtensions.Json);
        await client.PostAsJsonAsync("/api/transactions", new
        {
            type = "Expense",
            amount = 500m,
            categoryId = foodId,
            creditCardId = card!.Id,
            transactionDate = ThisMonth(10),
        });

        var chat = await client.PostAsJsonAsync("/api/assistant/chat", new { message = "Kredi kartı borcum ne kadar?" });
        var response = await chat.Content.ReadFromJsonAsync<ChatResponse>(ApiTestExtensions.Json);

        response!.Answer.Should().Contain("500");
    }

    [Fact]
    public async Task Chat_MixedQuestion_RunsSemanticRetrievalWithoutError()
    {
        var client = await _factory.RegisterClientAsync();
        var foodId = await client.GetCategoryIdAsync("Food");
        await AddExpenseAsync(client, foodId, 500m);

        // "summarize" (semantic) + "budget" (structured) => Mixed => exercises pgvector retrieval.
        var chat = await client.PostAsJsonAsync("/api/assistant/chat",
            new { message = "Summarize my budget and where I should be careful" });

        chat.StatusCode.Should().Be(HttpStatusCode.OK);
        var response = await chat.Content.ReadFromJsonAsync<ChatResponse>(ApiTestExtensions.Json);
        response!.Answer.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetConversation_WhenOwnedByAnotherUser_ReturnsNotFound()
    {
        var clientA = await _factory.RegisterClientAsync();
        var foodId = await clientA.GetCategoryIdAsync("Food");
        await AddExpenseAsync(clientA, foodId, 200m);
        var chat = await clientA.PostAsJsonAsync("/api/assistant/chat", new { message = "What is my balance?" });
        var response = await chat.Content.ReadFromJsonAsync<ChatResponse>(ApiTestExtensions.Json);

        var clientB = await _factory.RegisterClientAsync();
        var forbidden = await clientB.GetAsync($"/api/assistant/conversations/{response!.ConversationId}");

        forbidden.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Chat_DoesNotLeakAnotherUsersData()
    {
        // User A has a very distinctive amount.
        var clientA = await _factory.RegisterClientAsync();
        var foodA = await clientA.GetCategoryIdAsync("Food");
        await AddExpenseAsync(clientA, foodA, 4242m);

        // User B asks about their own finances; the answer must never contain A's amount.
        var clientB = await _factory.RegisterClientAsync();
        var foodB = await clientB.GetCategoryIdAsync("Food");
        await AddExpenseAsync(clientB, foodB, 111m);

        var chat = await clientB.PostAsJsonAsync("/api/assistant/chat", new { message = "How much did I spend this month?" });
        var response = await chat.Content.ReadFromJsonAsync<ChatResponse>(ApiTestExtensions.Json);

        response!.Answer.Should().NotContain("4242");
    }
}
