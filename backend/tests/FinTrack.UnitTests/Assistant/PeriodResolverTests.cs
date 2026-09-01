using FinTrack.Domain.Services;
using FluentAssertions;

namespace FinTrack.UnitTests.Assistant;

public class PeriodResolverTests
{
    private static readonly DateOnly Today = new(2026, 9, 15);

    [Fact]
    public void Resolve_NoTimeExpression_DefaultsToCurrentMonth()
    {
        var period = PeriodResolver.Resolve("Ne kadar harcadım?", Today);

        period.Label.Should().Be("bu ay");
        period.Start.Should().Be(new DateOnly(2026, 9, 1));
        period.End.Should().Be(new DateOnly(2026, 9, 30));
        period.MonthsSpan.Should().Be(1);
    }

    [Fact]
    public void Resolve_LastMonth_ReturnsPreviousMonthRange()
    {
        var period = PeriodResolver.Resolve("Geçen ay ne kadar harcadım?", Today);

        period.Label.Should().Be("geçen ay");
        period.Start.Should().Be(new DateOnly(2026, 8, 1));
        period.End.Should().Be(new DateOnly(2026, 8, 31));
    }

    [Fact]
    public void Resolve_Last3Months_SpansThreeMonths()
    {
        var period = PeriodResolver.Resolve("Son 3 ayda ne harcadım?", Today);

        period.Label.Should().Be("son 3 ay");
        period.Start.Should().Be(new DateOnly(2026, 7, 1));
        period.End.Should().Be(new DateOnly(2026, 9, 30));
        period.MonthsSpan.Should().Be(3);
    }

    [Fact]
    public void Resolve_ThisYear_StartsInJanuary()
    {
        var period = PeriodResolver.Resolve("Bu yıl toplam giderim nedir?", Today);

        period.Label.Should().Be("bu yıl");
        period.Start.Should().Be(new DateOnly(2026, 1, 1));
    }
}
