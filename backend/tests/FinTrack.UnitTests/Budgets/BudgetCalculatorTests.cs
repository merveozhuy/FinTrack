using FinTrack.Domain.Enums;
using FinTrack.Domain.Services;
using FluentAssertions;

namespace FinTrack.UnitTests.Budgets;

public class BudgetCalculatorTests
{
    [Fact]
    public void Calculate_WhenSpendingBelowThreshold_ShouldBeOk()
    {
        var result = BudgetCalculator.Calculate(monthlyLimit: 1000m, spent: 500m);

        result.Status.Should().Be(BudgetStatus.Ok);
        result.Remaining.Should().Be(500m);
        result.UsagePercentage.Should().Be(50m);
        result.IsThresholdReached.Should().BeFalse();
    }

    [Fact]
    public void Calculate_WhenSpendingReachesEightyPercent_ShouldWarn()
    {
        var result = BudgetCalculator.Calculate(monthlyLimit: 1000m, spent: 800m);

        result.Status.Should().Be(BudgetStatus.Warning);
        result.IsThresholdReached.Should().BeTrue();
        result.UsagePercentage.Should().Be(80m);
    }

    [Fact]
    public void Calculate_WhenSpendingExceedsLimit_ShouldMarkAsExceeded()
    {
        var result = BudgetCalculator.Calculate(monthlyLimit: 1000m, spent: 1250m);

        result.Status.Should().Be(BudgetStatus.Exceeded);
        result.Remaining.Should().Be(-250m);
        result.IsThresholdReached.Should().BeTrue();
    }

    [Fact]
    public void Calculate_WhenSpendingEqualsLimit_ShouldNotBeExceeded()
    {
        var result = BudgetCalculator.Calculate(monthlyLimit: 1000m, spent: 1000m);

        result.Status.Should().Be(BudgetStatus.Warning);
        result.Remaining.Should().Be(0m);
    }

    [Theory]
    [InlineData(1000, 0, 0)]
    [InlineData(1000, 250, 25)]
    [InlineData(1000, 1000, 100)]
    public void Calculate_ShouldComputeUsagePercentage(decimal limit, decimal spent, decimal expectedPercent)
    {
        var result = BudgetCalculator.Calculate(limit, spent);

        result.UsagePercentage.Should().Be(expectedPercent);
    }
}
