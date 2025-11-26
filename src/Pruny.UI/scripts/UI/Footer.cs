using Godot;

namespace Pruny.UI;

public partial class Footer : HBoxContainer
{
    private Label? _statusLabel;
    private ProgressBar? _progressBar;

    public override void _Ready()
    {
        _statusLabel = GetNode<Label>("StatusLabel");
        _progressBar = GetNode<ProgressBar>("ProgressBar");

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
        SetStatus($"Error: {errorMessage}");
    }

    private void OnPricesUpdated(int materialCount, string timestamp, string source)
    {
        SetStatus($"Prices updated: {materialCount} materials from {source}");
    }

    public void SetStatus(string message)
    {
        if (_statusLabel != null)
        {
            _statusLabel.Text = message;
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
