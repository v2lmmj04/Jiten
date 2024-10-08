using FluentAssertions;

namespace Jiten.Parser.Tests;

public class DeconjugatorTests
{
    [Theory]
    [InlineData("終わってしまった", new[] { "終わる" })]
    [InlineData("わからない", new[] { "わかる" })]
    public async Task DeconjugationTest(string text, string[] expectedResult)
    {
         Deconjugate(text).Select(r => r.Text).Should().Equal(expectedResult);
    }
    
    private HashSet<DeconjugationForm> Deconjugate(string text)
    {
        var deconjugator = new Deconjugator();
        return deconjugator.Deconjugate(text);
    }
}