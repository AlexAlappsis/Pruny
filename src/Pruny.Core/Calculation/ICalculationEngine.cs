namespace Pruny.Core.Calculation;

using Pruny.Core.Models;

public interface ICalculationEngine
{
    CalculationResult CalculateUnitCosts(
        List<ProductionLine> productionLines,
        Dictionary<string, Recipe> recipes,
        Dictionary<string, Building> buildings,
        WorkforceConfig workforceConfig,
        PriceSourceRegistry priceRegistry);
}
