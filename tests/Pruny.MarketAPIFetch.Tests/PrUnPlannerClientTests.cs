using FluentAssertions;
using Pruny.MarketAPIFetch;

namespace Pruny.MarketAPIFetch.Tests;

public class PrUnPlannerClientTests
{
    [Fact]
    public void Constructor_WithNullHttpClient_ThrowsArgumentNullException()
    {
        var options = new PrUnPlannerClientOptions
        {
            BaseUrl = "http://api.example.com",
            ApiKey = "test-key"
        };

        var act = () => new PrUnPlannerClient(null!, options);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("httpClient");
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        var httpClient = new HttpClient();

        var act = () => new PrUnPlannerClient(httpClient, null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    [Fact]
    public void Constructor_WithEmptyBaseUrl_ThrowsArgumentException()
    {
        var httpClient = new HttpClient();
        var options = new PrUnPlannerClientOptions
        {
            BaseUrl = "",
            ApiKey = "test-key"
        };

        var act = () => new PrUnPlannerClient(httpClient, options);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*BaseUrl is required*");
    }

    [Fact]
    public void Constructor_WithEmptyApiKey_ThrowsArgumentException()
    {
        var httpClient = new HttpClient();
        var options = new PrUnPlannerClientOptions
        {
            BaseUrl = "http://api.example.com",
            ApiKey = ""
        };

        var act = () => new PrUnPlannerClient(httpClient, options);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*ApiKey is required*");
    }

    [Fact]
    public void Constructor_WithValidParameters_SetsTimeout()
    {
        var httpClient = new HttpClient();
        var options = new PrUnPlannerClientOptions
        {
            BaseUrl = "http://api.example.com",
            ApiKey = "test-key",
            Timeout = TimeSpan.FromSeconds(45)
        };

        var client = new PrUnPlannerClient(httpClient, options);

        httpClient.Timeout.Should().Be(TimeSpan.FromSeconds(45));
    }
}
