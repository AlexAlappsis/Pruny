using Godot;

namespace Pruny.UI;

public partial class MainUI : Control
{
    private Control? _contentContainer;
    private Node? _currentContent;

    [Export]
    public PackedScene? DashboardScene { get; set; }

    [Export]
    public PackedScene? WorkspaceManagerScene { get; set; }

    public override void _Ready()
    {
        _contentContainer = GetNode<Control>("VBoxContainer/ContentContainer");

        LoadDashboard();
    }

    public void LoadDashboard()
    {
        if (DashboardScene == null)
        {
            GD.PrintErr("MainUI: Dashboard scene not assigned");
            return;
        }

        LoadContent(DashboardScene);
    }

    public void LoadWorkspaceManager()
    {
        if (WorkspaceManagerScene == null)
        {
            GD.PrintErr("MainUI: WorkspaceManager scene not assigned");
            return;
        }

        LoadContent(WorkspaceManagerScene);
    }

    private void LoadContent(PackedScene scene)
    {
        if (_contentContainer == null)
            return;

        if (_currentContent != null)
        {
            _currentContent.QueueFree();
            _currentContent = null;
        }

        var instance = scene.Instantiate();
        _contentContainer.AddChild(instance);
        _currentContent = instance;

        GD.Print($"MainUI: Loaded content - {scene.ResourcePath}");
    }
}
