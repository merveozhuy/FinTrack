using FinTrack.Domain.Enums;
using FinTrack.Domain.Services;
using FluentAssertions;

namespace FinTrack.UnitTests.Assistant;

public class QueryClassifierTests
{
    [Theory]
    [InlineData("How much did I spend this month?")]
    [InlineData("What is my net balance?")]
    [InlineData("Bu ay ne kadar harcadım?")]
    public void Classify_StructuredQuestions_ReturnsStructured(string question)
    {
        QueryClassifier.Classify(question).Should().Be(QueryType.Structured);
    }

    [Theory]
    [InlineData("Describe my habits")]
    [InlineData("Harcama alışkanlıklarımı açıkla")]
    public void Classify_SemanticQuestions_ReturnsSemantic(string question)
    {
        QueryClassifier.Classify(question).Should().Be(QueryType.Semantic);
    }

    [Fact]
    public void Classify_WhenBothSignalsPresent_ReturnsMixed()
    {
        QueryClassifier.Classify("Summarize which categories I exceeded my budget on")
            .Should().Be(QueryType.Mixed);
    }

    [Fact]
    public void Classify_WhenNoStrongSignal_DefaultsToMixed()
    {
        QueryClassifier.Classify("Tell me about last month").Should().Be(QueryType.Mixed);
    }
}
