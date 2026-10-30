using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shortener.API.Endpoints;
using Shortener.API.Errors;
using Shortener.API.Exceptions;
using Shortener.API.Infrastructure.Configurations;
using Shortener.API.Infrastructure.Extensions;
using Shortener.API.Services.Interface;

namespace Shortener.UnitTests;

public sealed class RegressionTests
{
    [Fact]
    public void DatabaseTimeout_PreservesCodeAndReturnsServiceUnavailable()
    {
        var error = ErrorCatalog.Get(ExceptionTranslator.Translate(new TimeoutException()));
        Assert.Equal(ErrorCodes.DatabaseTimeout, error.Code);
        Assert.Equal(503, error.StatusCode);
    }

    [Fact]
    public void ValidationFailure_ReturnsValidationProblemWithMetadata()
    {
        var errors = new Dictionary<string, string[]> { ["LongUrl"] = ["Invalid URL"] };
        var result = Assert.IsType<ValidationProblem>(new ErrorTranslator()
            .ToValidationResult(errors, new DefaultHttpContext()));
        Assert.Equal(errors["LongUrl"], result.ProblemDetails.Errors["LongUrl"]);
        Assert.Equal(ErrorCodes.ValidationFailed, result.ProblemDetails.Extensions["code"]);
        Assert.Equal(400, result.StatusCode);
    }

    [Theory]
    [InlineData("short")]
    [InlineData("ABCDEFGHIJ")]
    [InlineData("۱۲۳۴۵۶۷۸۹")]
    [InlineData("abcdefgh!")]
    public async Task InvalidCode_IsRejectedBeforeResolution(string code)
    {
        var service = Substitute.For<IRedirectService>();
        var result = await RedirectEndpoints.RedirectToLongUrl(code, service,
            new ErrorTranslator(), new DefaultHttpContext(), CancellationToken.None);
        Assert.Equal(400, Assert.IsType<ProblemHttpResult>(result).StatusCode);
        Assert.Empty(service.ReceivedCalls());
    }

    [Fact]
    public void DisabledCache_DoesNotRequireRedisConfiguration()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["CacheSettings:UseCache"] = "false",
            ["CacheSettings:ExpirationInMinutes"] = "30",
            ["CacheSettings:LocalCacheExpirationInMinutes"] = "5",
            ["ConnectionStrings:Redis"] = null
        });
        builder.AddCache();
        Assert.DoesNotContain(builder.Services, x => x.ServiceType == typeof(IDistributedCache));
    }

    [Theory]
    [InlineData("")]
    [InlineData("/relative")]
    [InlineData("ftp://example.com")]
    [InlineData("https://example.com?q=1")]
    [InlineData("https://example.com#fragment")]
    public void InvalidBaseUrl_FailsOptionsValidation(string baseUrl)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ShortenerSettings:BaseUrl"] = baseUrl,
            ["ShortenerSettings:SecretKey"] = Convert.ToBase64String(new byte[32])
        });
        builder.AddApplicationServices();
        using var provider = builder.Services.BuildServiceProvider();
        Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<ShortenerSettings>>().Value);
    }
}
