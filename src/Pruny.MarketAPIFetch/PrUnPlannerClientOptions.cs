namespace Pruny.MarketAPIFetch;

public class PrUnPlannerClientOptions
{
    public required string BaseUrl { get; set; }
    public required string ApiKey { get; set; }
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
    public int MaxRetries { get; set; } = 3;
}
