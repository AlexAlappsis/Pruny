namespace Pruny.Core.Calculation;

using Pruny.Core.Models;

public class DependencyResolver
{
    public List<string> ResolveDependencyOrder(
        List<ProductionLine> productionLines,
        Dictionary<string, Recipe> recipes)
    {
        var graph = BuildDependencyGraph(productionLines, recipes);
        var visited = new HashSet<string>();
        var recursionStack = new HashSet<string>();
        var result = new List<string>();

        foreach (var lineId in graph.Keys)
        {
            if (!visited.Contains(lineId))
            {
                TopologicalSort(lineId, graph, visited, recursionStack, result, new List<string>());
            }
        }

        return result;
    }

    public HashSet<string> FindDependentLines(
        string sourceLineId,
        List<ProductionLine> productionLines,
        Dictionary<string, Recipe> recipes)
    {
        var graph = BuildDependencyGraph(productionLines, recipes);
        var dependents = new HashSet<string>();

        FindDependentsRecursive(sourceLineId, graph, dependents);

        return dependents;
    }

    private void FindDependentsRecursive(
        string lineId,
        Dictionary<string, List<string>> graph,
        HashSet<string> dependents)
    {
        foreach (var (currentLineId, dependencies) in graph)
        {
            if (dependencies.Contains(lineId) && dependents.Add(currentLineId))
            {
                FindDependentsRecursive(currentLineId, graph, dependents);
            }
        }
    }

    private Dictionary<string, List<string>> BuildDependencyGraph(
        List<ProductionLine> productionLines,
        Dictionary<string, Recipe> recipes)
    {
        var graph = new Dictionary<string, List<string>>();
        var linesByMaterial = new Dictionary<string, string>();

        foreach (var line in productionLines)
        {
            graph[line.Id] = new List<string>();

            if (recipes.TryGetValue(line.RecipeId, out var recipe))
            {
                foreach (var output in recipe.Outputs)
                {
                    linesByMaterial[output.MaterialId] = line.Id;
                }
            }
        }

        foreach (var line in productionLines)
        {
            if (!recipes.TryGetValue(line.RecipeId, out var recipe))
                continue;

            foreach (var input in recipe.Inputs)
            {
                if (line.InputPriceSources.TryGetValue(input.MaterialId, out var priceSource) &&
                    priceSource.Type == PriceSourceType.ProductionLine)
                {
                    graph[line.Id].Add(priceSource.SourceIdentifier);
                }
            }
        }

        return graph;
    }

    private void TopologicalSort(
        string lineId,
        Dictionary<string, List<string>> graph,
        HashSet<string> visited,
        HashSet<string> recursionStack,
        List<string> result,
        List<string> path)
    {
        visited.Add(lineId);
        recursionStack.Add(lineId);
        path.Add(lineId);

        if (graph.TryGetValue(lineId, out var dependencies))
        {
            foreach (var dependency in dependencies)
            {
                if (!visited.Contains(dependency))
                {
                    TopologicalSort(dependency, graph, visited, recursionStack, result, new List<string>(path));
                }
                else if (recursionStack.Contains(dependency))
                {
                    var cycleStart = path.IndexOf(dependency);
                    var cycle = path.Skip(cycleStart).Concat(new[] { dependency }).ToList();
                    throw new CircularDependencyException(cycle);
                }
            }
        }

        recursionStack.Remove(lineId);
        result.Add(lineId);
    }
}
