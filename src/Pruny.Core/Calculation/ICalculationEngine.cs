namespace Pruny.Core.Calculation;

using Pruny.Core.Models;

public interface ICalculationEngine
{
    CalculationResult CalculateProductionLines(
        List<ProductionLine> productionLines,
        Dictionary<string, Recipe> recipes,
        Dictionary<string, Building> buildings,
        Dictionary<string, WorkforceTypeConfig> workforceConfigs,
        PriceSourceRegistry priceRegistry,
        HashSet<string> buildingsRequiringWholeUnitRounding);
}
