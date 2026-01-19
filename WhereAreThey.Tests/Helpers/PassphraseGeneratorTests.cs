using WhereAreThey.Helpers;

namespace WhereAreThey.Tests.Helpers;

public class PassphraseGeneratorTests
{
    [Fact]
    public void Generate_ShouldReturnValidFormat()
    {
        // Act
        var passphrase = PassphraseGenerator.Generate();

        // Assert
        Assert.NotNull(passphrase);
        var parts = passphrase.Split('-');
        Assert.Equal(4, parts.Length);
        
        // Parts 1-3 should be strings (adjectives/nouns)
        Assert.All(parts.Take(3), part => Assert.True(part.Length > 0));
        
        // Part 4 should be a number between 10 and 99
        Assert.True(int.TryParse(parts[3], out var number));
        Assert.InRange(number, 10, 99);
    }

    [Fact]
    public void Generate_ShouldBeRandom()
    {
        // Act
        var passphrase1 = PassphraseGenerator.Generate();
        var passphrase2 = PassphraseGenerator.Generate();

        // Assert
        Assert.NotEqual(passphrase1, passphrase2);
    }

    [Fact]
    public void Generate_ShouldMeetMinimumLength()
    {
        // Act
        var passphrase = PassphraseGenerator.Generate();

        // Assert
        // Minimum length in LocationReportValidator is 8
        Assert.True(passphrase.Length >= 8);
    }
}
