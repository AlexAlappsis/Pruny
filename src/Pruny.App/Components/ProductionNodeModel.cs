using Blazor.Diagrams.Core.Geometry;
using Blazor.Diagrams.Core.Models;
using Pruny.Core.Models;

namespace Pruny.App.Components;

public class ProductionNodeModel : NodeModel
{
    public ProductionLine? ProductionLine { get; }
    public ProductionLineCalculation? Calculation { get; set; }

    public ProductionNodeModel(ProductionLine productionLine, Point? position = null) : base(position)
    {
        ProductionLine = productionLine;
        Title = productionLine.Name;
        
        // Add default ports
        AddPort(PortAlignment.Left);
        AddPort(PortAlignment.Right);
    }
}
