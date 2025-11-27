using System.Globalization;
using System.Net;
using CsvHelper;
using CsvHelper.Configuration;
using Pruny.Library.Models;
using Pruny.Library.Services;
using Pruny.MarketAPIFetch.Models;

namespace Pruny.MarketAPIFetch;

public class PrUnPlannerClient : IMarketDataProvider
{
    private readonly HttpClient _httpClient;
    private readonly PrUnPlannerClientOptions _options;

    public PrUnPlannerClient(HttpClient httpClient, PrUnPlannerClientOptions options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options ?? throw new ArgumentNullException(nameof(options));

        if (string.IsNullOrWhiteSpace(_options.BaseUrl))
            throw new ArgumentException("BaseUrl is required.", nameof(options));

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new ArgumentException("ApiKey is required.", nameof(options));

        _httpClient.Timeout = _options.Timeout;
    }

    public async Task<MarketPriceData> FetchMarketPricesAsync(CancellationToken cancellationToken = default)
    {
        var url = BuildRequestUrl();
        var csvContent = await FetchWithRetryAsync(url, cancellationToken);
        var prices = ParseCsvResponse(csvContent);

        return new MarketPriceData
        {
            Prices = prices,
            FetchedAt = DateTimeOffset.UtcNow,
            Source = "PrUnPlanner API"
        };
    }

    private string BuildRequestUrl()
    {
        var baseUrl = _options.BaseUrl.TrimEnd('/');
        return $"{baseUrl}/csv/exchange?api_key={Uri.EscapeDataString(_options.ApiKey)}";
    }

    private async Task<string> FetchWithRetryAsync(string url, CancellationToken cancellationToken)
    {
        Exception? lastException = null;
        var retryDelays = new[] { TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4) };

        for (int attempt = 0; attempt <= _options.MaxRetries; attempt++)
        {
            try
            {
                var response = await _httpClient.GetAsync(url, cancellationToken);

                if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
                {
                    throw new HttpRequestException($"Authentication failed: {response.StatusCode}. Check your API key.");
                }

                if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    throw new HttpRequestException($"Bad request: {response.StatusCode}. {errorContent}");
                }

                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsStringAsync(cancellationToken);
            }
            catch (HttpRequestException ex) when (IsRetriableError(ex) && attempt < _options.MaxRetries)
            {
                lastException = ex;
                await Task.Delay(retryDelays[Math.Min(attempt, retryDelays.Length - 1)], cancellationToken);
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested && attempt < _options.MaxRetries)
            {
                lastException = ex;
                await Task.Delay(retryDelays[Math.Min(attempt, retryDelays.Length - 1)], cancellationToken);
            }
        }

        throw new HttpRequestException($"Failed to fetch market data after {_options.MaxRetries + 1} attempts.", lastException);
    }

    private static bool IsRetriableError(HttpRequestException ex)
    {
        if (ex.StatusCode == null)
            return true;

        var statusCode = (int)ex.StatusCode.Value;
        return statusCode >= 500 && statusCode < 600;
    }

    private static List<MarketPrice> ParseCsvResponse(string csvContent)
    {
        if (string.IsNullOrWhiteSpace(csvContent))
            throw new InvalidDataException("CSV response is empty.");

        try
        {
            using var reader = new StringReader(csvContent);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                MissingFieldFound = null,
            });

            var records = csv.GetRecords<ExchangeCsvRow>().ToList();

            return records.Select(r => new MarketPrice
            {
                Ticker = r.Ticker,
                ExchangeCode = r.ExchangeCode,
                Ask = r.Ask,
                Bid = r.Bid,
                Average = r.Avg,
                Supply = (int?)r.Supply,
                Demand = (int?)r.Demand,
                Traded = (int?)r.Traded
            }).ToList();
        }
        catch (CsvHelperException ex)
        {
            throw new InvalidDataException("Failed to parse CSV response from PrUnPlanner API.", ex);
        }
    }
}
