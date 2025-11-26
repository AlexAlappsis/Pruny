using Godot;

namespace Pruny.UI;

public partial class Footer : HBoxContainer
{
    private Label? _statusLabel;
    private ProgressBar? _progressBar;
    private Godot.Timer? _errorTimer;

    public override void _Ready()
    {
        _statusLabel = GetNode<Label>("StatusLabel");
        _progressBar = GetNode<ProgressBar>("ProgressBar");

        _errorTimer = new Godot.Timer();
        _errorTimer.WaitTime = 5.0;
        _errorTimer.OneShot = true;
        _errorTimer.Timeout += OnErrorTimeout;
        AddChild(_errorTimer);

        var sessionManager = GetNode<SessionManager>("/root/SessionManager");
        if (sessionManager != null)
        {
            sessionManager.SessionStateChangedSignal += OnSessionStateChanged;
            sessionManager.CalculationErrorSignal += OnCalculationError;
            sessionManager.PricesUpdatedSignal += OnPricesUpdated;
        }

        SetStatus("Ready");
        HideProgress();
    }

    public override void _ExitTree()
    {
        var sessionManager = GetNode<SessionManager>("/root/SessionManager");
        if (sessionManager != null)
        {
            sessionManager.SessionStateChangedSignal -= OnSessionStateChanged;
            sessionManager.CalculationErrorSignal -= OnCalculationError;
            sessionManager.PricesUpdatedSignal -= OnPricesUpdated;
        }
    }

    private void OnSessionStateChanged(bool isInitialized, bool isCalculating, string message)
    {
        SetStatus(message);

        if (isCalculating)
        {
            ShowProgress();
        }
        else
        {
            HideProgress();
        }
    }

    private void OnCalculationError(string errorMessage)
    {
        SetStatusError($"Error: {errorMessage}");
        _errorTimer?.Start();
    }

    private void OnPricesUpdated(int materialCount, string timestamp, string source)
    {
        SetStatusSuccess($"Prices updated: {materialCount} materials from {source}");
    }

    private void OnErrorTimeout()
    {
        SetStatus("Ready");
    }

    public void SetStatus(string message)
    {
        if (_statusLabel != null)
        {
            _statusLabel.Text = message;
            _statusLabel.AddThemeColorOverride("font_color", new Color(1, 1, 1));
        }
    }

    public void SetStatusError(string message)
    {
        if (_statusLabel != null)
        {
            _statusLabel.Text = message;
            _statusLabel.AddThemeColorOverride("font_color", new Color(1, 0.3f, 0.3f));
        }
    }

    public void SetStatusSuccess(string message)
    {
        if (_statusLabel != null)
        {
            _statusLabel.Text = message;
            _statusLabel.AddThemeColorOverride("font_color", new Color(0.3f, 1, 0.3f));
        }
    }

    public void ShowProgress()
    {
        if (_progressBar != null)
        {
            _progressBar.Visible = true;
        }
    }

    public void HideProgress()
    {
        if (_progressBar != null)
        {
            _progressBar.Visible = false;
        }
    }
}
