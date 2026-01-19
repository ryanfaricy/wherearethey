using WhereAreThey.Services;

namespace WhereAreThey.Tests.Services;

public class HashUtilsTests
{
    [Fact]
    public void ComputeHash_ShouldReturnEmptyString_WhenEmailIsIsNullOrWhiteSpace()
    {
        // Act & Assert
        Assert.Equal(string.Empty, HashUtils.ComputeHash(null!));
        Assert.Equal(string.Empty, HashUtils.ComputeHash(""));
        Assert.Equal(string.Empty, HashUtils.ComputeHash("   "));
    }

    [Fact]
    public void ComputeHash_ShouldBeCaseInsensitive()
    {
        // Arrange
        var email1 = "Test@Example.Com";
        var email2 = "test@example.com";

        // Act
        var hash1 = HashUtils.ComputeHash(email1);
        var hash2 = HashUtils.ComputeHash(email2);

        // Assert
        Assert.Equal(hash1, hash2);
        Assert.NotEmpty(hash1);
    }

    [Fact]
    public void ComputeHash_ShouldTrimWhitespace()
    {
        // Arrange
        var email1 = "test@example.com";
        var email2 = "  test@example.com  ";

        // Act
        var hash1 = HashUtils.ComputeHash(email1);
        var hash2 = HashUtils.ComputeHash(email2);

        // Assert
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void ComputeHash_ShouldProduceValidSha256Hex()
    {
        // Arrange
        var email = "test@example.com";
        // SHA256 of "test@example.com" is 973DFE463EC85785F5F95AF5BA3906EEDB2D931C24E69824A89EA65DBA4E813B
        var expectedHash = "973DFE463EC85785F5F95AF5BA3906EEDB2D931C24E69824A89EA65DBA4E813B";

        // Act
        var result = HashUtils.ComputeHash(email);

        // Assert
        Assert.Equal(expectedHash, result);
    }
}
