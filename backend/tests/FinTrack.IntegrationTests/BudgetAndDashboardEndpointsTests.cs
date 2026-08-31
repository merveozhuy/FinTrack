using System.Net;
using System.Net.Http.Json;
using FinTrack.Application.Features.Budgets.Dtos;
using FinTrack.Application.Features.Dashboard.Dtos;
using FinTrack.Domain.Enums;
using FluentAssertions;

namespace FinTrack.IntegrationTests;

[Collection(ApiCollection.Name)]
public class BudgetAndDashboardEndpointsTests
{
    private readonly ApiFactory _factory;

    public BudgetAndDashboardEndpointsTests(ApiFactory factory) => _factory = factory;

    private static object Expense(Guid categoryId, decimal amount, string date) =>
        new { type = "Expense", amount, categoryId, description = "test", transactionDate = date };

    private static object Income(Guid categoryId, decimal amount, string date) =>
        new { type = "Income", amount, categoryId, description = "test", transactionDate = date };

    [Fact]
    public async Task CreateBudget_WithDuplicateCategoryAndMonth_ReturnsConflict()
    {
        var client = await _factory.RegisterClientAsync();
        var foodId = await client.GetCategoryIdAsync("Food");
        var payload = new { categoryId = foodId, year = 2026, month = 8, monthlyLimit = 1000m };

        var first = await client.PostAsJsonAsync("/api/budgets", payload);
        var second = await client.PostAsJsonAsync("/api/budgets", payload);

        first.StatusCode.Should().Be(HttpStatusCode.Created);
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateBudget_ForIncomeCategory_ReturnsBadRequest()
    {
        var client = await _factory.RegisterClientAsync();
        var salaryId = await client.GetCategoryIdAsync("Salary");

        var response = await client.PostAsJsonAsync("/api/budgets",
            new { categoryId = salaryId, year = 2026, month = 8, monthlyLimit = 1000m });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetBudgets_WhenSpendingExceedsLimit_MarksAsExceeded()
    {
        var client = await _factory.RegisterClientAsync();
        var foodId = await client.GetCategoryIdAsync("Food");

        await client.PostAsJsonAsync("/api/budgets",
            new { categoryId = foodId, year = 2026, month = 9, monthlyLimit = 1000m });
        await client.PostAsJsonAsync("/api/transactions", Expense(foodId, 1200m, "2026-09-05"));

        var budgets = await client.GetFromJsonAsync<List<BudgetDto>>("/api/budgets/2026/9", ApiTestExtensions.Json);

        budgets.Should().ContainSingle();
        var food = budgets!.Single();
        food.Spent.Should().Be(1200m);
        food.Remaining.Should().Be(-200m);
        food.Status.Should().Be(BudgetStatus.Exceeded);
    }

    [Fact]
    public async Task GetDashboard_ReturnsCorrectMonthlyTotals()
    {
        var client = await _factory.RegisterClientAsync();
        var foodId = await client.GetCategoryIdAsync("Food");
        var salaryId = await client.GetCategoryIdAsync("Salary");

        await client.PostAsJsonAsync("/api/transactions", Income(salaryId, 5000m, "2026-10-01"));
        await client.PostAsJsonAsync("/api/transactions", Expense(foodId, 1200m, "2026-10-05"));

        var dashboard = await client.GetFromJsonAsync<DashboardDto>(
            "/api/dashboard?year=2026&month=10", ApiTestExtensions.Json);

        dashboard!.TotalIncome.Should().Be(5000m);
        dashboard.TotalExpense.Should().Be(1200m);
        dashboard.NetBalance.Should().Be(3800m);
        dashboard.ExpenseByCategory.Should().ContainSingle(c => c.CategoryName == "Food");
    }

    [Fact]
    public async Task GetDashboard_WithInvalidMonth_ReturnsBadRequest()
    {
        var client = await _factory.RegisterClientAsync();

        var response = await client.GetAsync("/api/dashboard?year=2026&month=13");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
