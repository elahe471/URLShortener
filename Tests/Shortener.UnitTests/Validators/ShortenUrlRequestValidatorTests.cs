
using FluentAssertions;
using global::Shortener.API.Endpoints.Contracts;
using Microsoft.Extensions.Time.Testing;

namespace Shortener.UnitTests.Validators
{


    public sealed class ShortenUrlRequestValidatorTests
    {
        private readonly FakeTimeProvider _timeProvider;
        private readonly ShortenUrlRequestValidator _sut;

        public ShortenUrlRequestValidatorTests()
        {
            _timeProvider = new FakeTimeProvider(
                new DateTimeOffset(
                    2026, 9, 10,
                    12, 0, 0,
                    TimeSpan.Zero));

            _sut = new ShortenUrlRequestValidator(
                _timeProvider);
        }

        [Fact]
        public async Task Validate_WithFutureExpirationDate_ShouldBeValid()
        {
            // Arrange
            var request = new ShortenUrlRequest(
                "https://example.com",
                _timeProvider.GetUtcNow().AddDays(1));

            // Act
            var result = await _sut.ValidateAsync(request);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public async Task Validate_WithPastExpirationDate_ShouldBeInvalid()
        {
            // Arrange
            var request = new ShortenUrlRequest(
                "https://example.com",
                _timeProvider.GetUtcNow().AddMinutes(-1));

            // Act
            var result = await _sut.ValidateAsync(request);

            // Assert
            result.IsValid.Should().BeFalse();

            result.Errors.Should()
                .Contain(x =>
                    x.PropertyName == nameof(
                        ShortenUrlRequest.ExpirationDate));
        }

        [Fact]
        public async Task Validate_WithCurrentExpirationDate_ShouldBeInvalid()
        {
            // Arrange
            var request = new ShortenUrlRequest(
                "https://example.com",
                _timeProvider.GetUtcNow());

            // Act
            var result = await _sut.ValidateAsync(request);

            // Assert
            result.IsValid.Should().BeFalse();
        }
    }
}
