namespace Pruny.Core.Calculation;

public class CircularDependencyException : Exception
{
    public List<string> DependencyCycle { get; }

    public CircularDependencyException(List<string> cycle)
        : base($"Circular dependency detected: {string.Join(" -> ", cycle)}")
    {
        DependencyCycle = cycle;
    }
}
