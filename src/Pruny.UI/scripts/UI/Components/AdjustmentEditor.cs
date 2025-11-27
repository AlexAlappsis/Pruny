using Godot;
using Pruny.Core.Models;

namespace Pruny.UI.Components;

public partial class AdjustmentEditor : HBoxContainer
{
    public event Action<Adjustment>? AdjustmentChanged;

    [Signal]
    public delegate void DeleteRequestedEventHandler();

    private OptionButton? _typeDropdown;
    private SpinBox? _valueInput;
    private Button? _deleteButton;

    public override void _Ready()
    {
        _typeDropdown = GetNode<OptionButton>("TypeDropdown");
        _valueInput = GetNode<SpinBox>("ValueInput");
        _deleteButton = GetNode<Button>("DeleteButton");

        SetupTypeDropdown();
        SetupValueInput();

        _typeDropdown.ItemSelected += OnTypeSelected;
        _valueInput.ValueChanged += OnValueChanged;
        _deleteButton.Pressed += OnDeletePressed;
    }

    private void SetupTypeDropdown()
    {
        _typeDropdown?.Clear();
        _typeDropdown?.AddItem("Percentage", (int)AdjustmentType.Percentage);
        _typeDropdown?.AddItem("Flat", (int)AdjustmentType.Flat);
    }

    private void SetupValueInput()
    {
        if (_valueInput == null) return;

        _valueInput.MinValue = -999999;
        _valueInput.MaxValue = 999999;
        _valueInput.Step = 0.01;
        _valueInput.Value = 0;
    }

    public void SetAdjustment(Adjustment adjustment)
    {
        _typeDropdown?.Select((int)adjustment.Type);
        if (_valueInput != null)
            _valueInput.Value = (double)adjustment.Value;
    }

    public Adjustment GetAdjustment()
    {
        if (_typeDropdown == null || _valueInput == null)
            throw new InvalidOperationException("AdjustmentEditor not initialized");

        return new Adjustment
        {
            Type = (AdjustmentType)_typeDropdown.Selected,
            Value = (decimal)_valueInput.Value
        };
    }

    private void OnTypeSelected(long index)
    {
        AdjustmentChanged?.Invoke(GetAdjustment());
    }

    private void OnValueChanged(double value)
    {
        AdjustmentChanged?.Invoke(GetAdjustment());
    }

    private void OnDeletePressed()
    {
        EmitSignal(SignalName.DeleteRequested);
    }

    public override void _ExitTree()
    {
        if (_typeDropdown != null)
            _typeDropdown.ItemSelected -= OnTypeSelected;
        if (_valueInput != null)
            _valueInput.ValueChanged -= OnValueChanged;
        if (_deleteButton != null)
            _deleteButton.Pressed -= OnDeletePressed;
    }
}
