using Godot;

namespace Pruny.UI.Components;

public partial class EfficiencyModifiersEditor : VBoxContainer
{
    private VBoxContainer? _modifiersContainer;
    private Button? _addModifierButton;

    private List<HBoxContainer> _modifierRows = new();

    public override void _Ready()
    {
        _modifiersContainer = GetNode<VBoxContainer>("ModifiersContainer");
        _addModifierButton = GetNode<Button>("AddModifierButton");

        _addModifierButton.Pressed += OnAddModifierPressed;
    }

    private void OnAddModifierPressed()
    {
        AddModifierRow(0.0m);
    }

    private void AddModifierRow(decimal value)
    {
        var row = new HBoxContainer();

        var label = new Label { Text = "Modifier:" };
        row.AddChild(label);

        var spinBox = new SpinBox
        {
            MinValue = -100,
            MaxValue = 500,
            Step = 0.01,
            Value = (double)value,
            CustomMinimumSize = new Vector2(150, 0)
        };
        row.AddChild(spinBox);

        var percentLabel = new Label { Text = "%" };
        row.AddChild(percentLabel);

        var deleteButton = new Button { Text = "Remove" };
        deleteButton.Pressed += () =>
        {
            _modifierRows.Remove(row);
            row.QueueFree();
        };
        row.AddChild(deleteButton);

        _modifiersContainer?.AddChild(row);
        _modifierRows.Add(row);
    }

    public void SetModifiers(List<decimal> modifiers)
    {
        ClearModifiers();

        foreach (var modifier in modifiers)
        {
            AddModifierRow(modifier);
        }
    }

    public List<decimal> GetModifiers()
    {
        var modifiers = new List<decimal>();

        foreach (var row in _modifierRows)
        {
            var spinBox = row.GetChild<SpinBox>(1);
            modifiers.Add((decimal)spinBox.Value);
        }

        return modifiers;
    }

    private void ClearModifiers()
    {
        foreach (var row in _modifierRows)
            row.QueueFree();
        _modifierRows.Clear();
    }

    public override void _ExitTree()
    {
        if (_addModifierButton != null)
            _addModifierButton.Pressed -= OnAddModifierPressed;
    }
}
