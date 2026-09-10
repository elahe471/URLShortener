using FluentAssertions;
using Microsoft.Extensions.Options;
using Shortener.API.Infrastructure.Configurations;
using Shortener.API.Services;

namespace Shortener.UnitTests.Services;


public class ShortCodeGeneratorTests
{
    private readonly ShortCodeGenerator _sut;

    public ShortCodeGeneratorTests()
    {
        var secretBytes = new byte[32];

        for (var i = 0; i < secretBytes.Length; i++)
        {
            secretBytes[i] = (byte)(i + 1);
        }

        var settings = new ShortenerSettings
        {
            SecretKey = Convert.ToBase64String(secretBytes),
            BaseUrl = "https://localhost"
        };

        _sut = new ShortCodeGenerator(Options.Create(settings));
    }

    /// <summary>
    /// Code length = 9
    /// </summary>
    [Fact]
    public void Generate_ShouldReturnNineCharacterCode()
    {
        // Act
        var result = _sut.Generate(1);

        // Assert
        result.Should().HaveLength(9);
    }
    /// <summary>
    /// Base62 only
    /// </summary>
    [Fact]
    public void Generate_ShouldContainOnlyBase62Characters()
    {
        // Act
        var result = _sut.Generate(12345);

        // Assert
        result.Should()
            .MatchRegex("^[0-9A-Za-z]{9}$");
    }
    /// <summary>
    /// Same input → same output
    /// </summary>
    [Fact]
    public void Generate_WithSameSequence_ShouldReturnSameCode()
    {
        // Act
        var first = _sut.Generate(1000);
        var second = _sut.Generate(1000);

        // Assert
        first.Should().Be(second);
    }
    /// <summary>
    /// Different unique input → different output
    /// </summary>
    [Fact]
    public void Generate_WithDifferentSequences_ShouldReturnDifferentCodes()
    {
        // Act
        var first = _sut.Generate(1000);
        var second = _sut.Generate(1001);

        // Assert
        first.Should().NotBe(second);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Generate_WithInvalidSequence_ShouldThrow(
        long sequence)
    {
        // Act
        var action = () => _sut.Generate(sequence);

        // Assert
        action.Should()
            .Throw<ArgumentOutOfRangeException>();
    }
    [Fact]
    public void Generate_WhenSequenceExceedsCapacity_ShouldThrow()
    {
        // Arrange
        var maxSequence = (1L << 52) - 1;
        var invalidSequence = maxSequence + 1;

        // Act
        var action = () =>
            _sut.Generate(invalidSequence);

        // Assert
        action.Should()
            .Throw<InvalidOperationException>();
    }
    [Fact]
    public void Generate_WithMaximumAllowedSequence_ShouldSucceed()
    {
        // Arrange
        var maxSequence = (1L << 52) - 1;

        // Act
        var result = _sut.Generate(maxSequence);

        // Assert
        result.Should().HaveLength(9);
    }
    [Fact]
    public void Generate_ForManyUniqueSequences_ShouldNotProduceDuplicates()
    {
        // Arrange
        const int count = 100_000;

        var generatedCodes = new HashSet<string>();

        // Act
        for (var sequence = 1; sequence <= count; sequence++)
        {
            var code = _sut.Generate(sequence);

            generatedCodes.Add(code);
        }

        // Assert
        generatedCodes.Should().HaveCount(count);
    }

}
