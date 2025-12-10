namespace Pruny.Core.Models;

public class PriceSourceRegistry
{
    private readonly Dictionary<string, Dictionary<string, decimal>> _prices = new();

    public void RegisterPrice(string materialId, string sourceIdentifier, decimal price)
    {
        if (!_prices.ContainsKey(materialId))
            _prices[materialId] = new();
        _prices[materialId][sourceIdentifier] = price;
    }

    public decimal GetPrice(string materialId, PriceSource source)
    {
        System.Diagnostics.Debug.WriteLine($"PriceRegistry.GetPrice: materialId={materialId}, type={source.Type}, sourceIdentifier={source.SourceIdentifier}");

        if (_prices.TryGetValue(materialId, out var sources))
        {
            System.Diagnostics.Debug.WriteLine($"  Found material, available sources: {string.Join(", ", sources.Keys)}");

            if (sources.TryGetValue(source.SourceIdentifier, out var price))
            {
                System.Diagnostics.Debug.WriteLine($"  Found source, price={price}");
                return ApplyAdjustments(price, source.Adjustments);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"  Source not found");
            }
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"  Material not found in registry");
        }

        return 0m;
    }

    private decimal ApplyAdjustments(decimal basePrice, List<Adjustment> adjustments)
    {
        var price = basePrice;
        foreach (var adjustment in adjustments)
        {
            price = adjustment.Type switch
            {
                AdjustmentType.Percentage => price * adjustment.Value,
                AdjustmentType.Flat => price + adjustment.Value,
                _ => price
            };
        }
        return price;
    }
}