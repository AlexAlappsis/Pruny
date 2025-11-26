using Godot;

namespace Pruny.UI;

public partial class Main : Node
{
    public override void _Ready()
    {
        GD.Print("Pruny UI initialized successfully!");
        GD.Print($"Godot version: {Engine.GetVersionInfo()["string"]}");
    }
}
