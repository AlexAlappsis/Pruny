using Godot;

namespace Pruny.UI.Dialogs;

public partial class ErrorDialog : AcceptDialog
{
    private Label? _messageLabel;
    private TextEdit? _detailsText;
    private Button? _detailsButton;
    private bool _detailsVisible = false;

    public override void _Ready()
    {
        _messageLabel = GetNode<Label>("VBoxContainer/MessageLabel");
        _detailsText = GetNode<TextEdit>("VBoxContainer/DetailsText");
        _detailsButton = GetNode<Button>("VBoxContainer/DetailsButton");

        _detailsButton.Pressed += OnDetailsButtonPressed;
        _detailsText.Visible = false;

        Confirmed += () => QueueFree();
        Canceled += () => QueueFree();
    }

    public void SetError(string title, string message, string? details = null)
    {
        Title = title;

        if (_messageLabel != null)
        {
            _messageLabel.Text = message;
        }

        if (_detailsText != null && !string.IsNullOrEmpty(details))
        {
            _detailsText.Text = details;
            if (_detailsButton != null)
            {
                _detailsButton.Visible = true;
            }
        }
        else
        {
            if (_detailsButton != null)
            {
                _detailsButton.Visible = false;
            }
        }
    }

    private void OnDetailsButtonPressed()
    {
        _detailsVisible = !_detailsVisible;

        if (_detailsText != null)
        {
            _detailsText.Visible = _detailsVisible;
        }

        if (_detailsButton != null)
        {
            _detailsButton.Text = _detailsVisible ? "Hide Details" : "Show Details";
        }
    }

    public static void Show(Node parent, string title, string message, string? details = null)
    {
        var dialog = GD.Load<PackedScene>("res://scenes/UI/Dialogs/ErrorDialog.tscn").Instantiate<ErrorDialog>();
        parent.AddChild(dialog);
        dialog.SetError(title, message, details);
        dialog.PopupCentered();
    }
}
