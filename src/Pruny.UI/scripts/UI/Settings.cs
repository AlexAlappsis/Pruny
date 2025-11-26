using Godot;

namespace Pruny.UI;

public partial class Settings : CenterContainer
{
    private MainUI? _mainUI;
    private LineEdit? _apiUrlInput;
    private LineEdit? _apiKeyInput;
    private SpinBox? _timeoutInput;
    private SpinBox? _retriesInput;
    private Button? _saveButton;
    private Button? _cancelButton;
    private Label? _statusLabel;

    public override void _Ready()
    {
        GD.Print("Settings: _Ready called");
        _mainUI = GetParent()?.GetParent()?.GetParent() as MainUI;

        if (_mainUI == null)
        {
            GD.PrintErr("Settings: Could not find MainUI in parent chain");
        }
        else
        {
            GD.Print("Settings: Successfully found MainUI");
        }

        try
        {
            _apiUrlInput = GetNode<LineEdit>("VBoxContainer/ScrollContainer/SettingsContainer/ApiUrlInput");
            _apiKeyInput = GetNode<LineEdit>("VBoxContainer/ScrollContainer/SettingsContainer/ApiKeyInput");
            _timeoutInput = GetNode<SpinBox>("VBoxContainer/ScrollContainer/SettingsContainer/TimeoutInput");
            _retriesInput = GetNode<SpinBox>("VBoxContainer/ScrollContainer/SettingsContainer/RetriesInput");
            _saveButton = GetNode<Button>("VBoxContainer/ButtonsContainer/SaveButton");
            _cancelButton = GetNode<Button>("VBoxContainer/ButtonsContainer/CancelButton");
            _statusLabel = GetNode<Label>("VBoxContainer/StatusLabel");

            _saveButton.Pressed += OnSavePressed;
            _cancelButton.Pressed += OnCancelPressed;

            LoadSettings();
            GD.Print("Settings: Initialization complete");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Settings: Error during initialization - {ex.Message}");
            GD.PrintErr($"Settings: Stack trace - {ex.StackTrace}");
        }
    }

    private void LoadSettings()
    {
        var config = AppConfig.Instance;

        if (_apiUrlInput != null)
            _apiUrlInput.Text = config.PrUnPlannerApiUrl;

        if (_apiKeyInput != null)
            _apiKeyInput.Text = config.PrUnPlannerApiKey;

        if (_timeoutInput != null)
            _timeoutInput.Value = config.ApiTimeout.TotalSeconds;

        if (_retriesInput != null)
            _retriesInput.Value = config.ApiMaxRetries;

        SetStatus("");
    }

    private void OnSavePressed()
    {
        if (!ValidateSettings())
        {
            return;
        }

        try
        {
            var config = AppConfig.Instance;

            config.PrUnPlannerApiUrl = _apiUrlInput?.Text ?? config.PrUnPlannerApiUrl;
            config.PrUnPlannerApiKey = _apiKeyInput?.Text ?? config.PrUnPlannerApiKey;
            config.ApiTimeout = TimeSpan.FromSeconds(_timeoutInput?.Value ?? 30);
            config.ApiMaxRetries = (int)(_retriesInput?.Value ?? 3);

            config.Save();

            SetStatus("Settings saved successfully!", new Color(0.3f, 1, 0.3f));
            GD.Print("Settings: Configuration saved");

            GetTree().CreateTimer(1.5).Timeout += () => _mainUI?.LoadDashboard();
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Settings: Failed to save - {ex.Message}");
            SetStatus($"Failed to save settings: {ex.Message}", new Color(1, 0.3f, 0.3f));
            Dialogs.ErrorDialog.Show(this, "Failed to Save Settings",
                "An error occurred while saving settings.",
                $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private void OnCancelPressed()
    {
        _mainUI?.LoadDashboard();
    }

    private bool ValidateSettings()
    {
        if (string.IsNullOrWhiteSpace(_apiUrlInput?.Text))
        {
            SetStatus("API URL cannot be empty", new Color(1, 0.3f, 0.3f));
            return false;
        }

        if (!Uri.TryCreate(_apiUrlInput.Text, UriKind.Absolute, out var uri) ||
            (uri.Scheme != "http" && uri.Scheme != "https"))
        {
            SetStatus("API URL must be a valid HTTP or HTTPS URL", new Color(1, 0.3f, 0.3f));
            return false;
        }

        if (string.IsNullOrWhiteSpace(_apiKeyInput?.Text))
        {
            SetStatus("API Key cannot be empty", new Color(1, 0.3f, 0.3f));
            return false;
        }

        if (_timeoutInput?.Value < 1 || _timeoutInput?.Value > 300)
        {
            SetStatus("Timeout must be between 1 and 300 seconds", new Color(1, 0.3f, 0.3f));
            return false;
        }

        if (_retriesInput?.Value < 0 || _retriesInput?.Value > 10)
        {
            SetStatus("Max retries must be between 0 and 10", new Color(1, 0.3f, 0.3f));
            return false;
        }

        return true;
    }

    private void SetStatus(string message, Color? color = null)
    {
        if (_statusLabel != null)
        {
            _statusLabel.Text = message;
            _statusLabel.AddThemeColorOverride("font_color", color ?? new Color(1, 1, 1));
        }
    }
}
