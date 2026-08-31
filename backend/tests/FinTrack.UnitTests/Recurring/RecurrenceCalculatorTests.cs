using FinTrack.Domain.Enums;
using FinTrack.Domain.Services;
using FluentAssertions;

namespace FinTrack.UnitTests.Recurring;

public class RecurrenceCalculatorTests
{
    [Fact]
    public void Next_Weekly_AddsSevenDays()
    {
        var next = RecurrenceCalculator.Next(new DateOnly(2026, 7, 1), RecurrenceFrequency.Weekly);

        next.Should().Be(new DateOnly(2026, 7, 8));
    }

    [Fact]
    public void Next_Monthly_AddsOneMonth()
    {
        var next = RecurrenceCalculator.Next(new DateOnly(2026, 7, 15), RecurrenceFrequency.Monthly);

        next.Should().Be(new DateOnly(2026, 8, 15));
    }

    [Fact]
    public void Next_Monthly_ClampsToShorterMonthEnd()
    {
        // 31 Jan + 1 month should land on the last day of February, not overflow.
        var next = RecurrenceCalculator.Next(new DateOnly(2026, 1, 31), RecurrenceFrequency.Monthly);

        next.Should().Be(new DateOnly(2026, 2, 28));
    }

    [Fact]
    public void Next_Yearly_AddsOneYear()
    {
        var next = RecurrenceCalculator.Next(new DateOnly(2026, 7, 1), RecurrenceFrequency.Yearly);

        next.Should().Be(new DateOnly(2027, 7, 1));
    }
}
