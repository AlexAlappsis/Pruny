using Godot;
using Pruny.Core.Models;

namespace Pruny.UI.Components;

public partial class WorkforceEditor : VBoxContainer
{
    private SessionManager? _sessionManager;
    private VBoxContainer? _workforceRowsContainer;
    private List<WorkforceRowData> _workforceRows = new();
    private List<WorkforceRequirement> _defaultWorkforce = new();

    private List<WorkforceRequirement>? _pendingDefaultWorkforce;
    private List<WorkforceRequirement>? _pendingWorkforceOverride;
    private Dictionary<WorkforceType, string>? _pendingWorkforceConfigMapping;
    private bool _hasPendingSettings;

    public override void _Ready()
    {
        _sessionManager = GetNode<SessionManager>("/root/SessionManager");
        _workforceRowsContainer = GetNode<VBoxContainer>("WorkforceRowsContainer");

        if (_pendingDefaultWorkforce != null)
        {
            _defaultWorkforce = _pendingDefaultWorkforce;
            _pendingDefaultWorkforce = null;

            if (_hasPendingSettings)
            {
                RebuildWorkforceRows(_pendingWorkforceOverride, _pendingWorkforceConfigMapping);
                _pendingWorkforceOverride = null;
                _pendingWorkforceConfigMapping = null;
                _hasPendingSettings = false;
            }
            else
            {
                RebuildWorkforceRows(null, null);
            }
        }
    }

    public void SetBuildingWorkforce(List<WorkforceRequirement> defaultWorkforce)
    {
        if (_workforceRowsContainer == null)
        {
            _pendingDefaultWorkforce = defaultWorkforce;
            return;
        }

        _defaultWorkforce = defaultWorkforce;
        RebuildWorkforceRows(null, null);
    }

    public void SetWorkforceSettings(
        List<WorkforceRequirement>? workforceOverride,
        Dictionary<WorkforceType, string>? workforceConfigMapping)
    {
        if (_workforceRowsContainer == null)
        {
            _pendingWorkforceOverride = workforceOverride;
            _pendingWorkforceConfigMapping = workforceConfigMapping;
            _hasPendingSettings = true;
            return;
        }

        RebuildWorkforceRows(workforceOverride, workforceConfigMapping);
    }

    private void RebuildWorkforceRows(
        List<WorkforceRequirement>? workforceOverride,
        Dictionary<WorkforceType, string>? workforceConfigMapping)
    {
        ClearRows();

        foreach (var requirement in _defaultWorkforce)
        {
            var overrideCount = workforceOverride?
                .FirstOrDefault(w => w.WorkforceType == requirement.WorkforceType)?.Count;

            string? configName = null;
            workforceConfigMapping?.TryGetValue(requirement.WorkforceType, out configName);

            AddWorkforceRow(requirement.WorkforceType, requirement.Count, overrideCount, configName);
        }
    }

    private void AddWorkforceRow(
        WorkforceType workforceType,
        int defaultCount,
        int? overrideCount,
        string? configName)
    {
        var rowContainer = new VBoxContainer();

        var headerRow = new HBoxContainer();
        var typeLabel = new Label
        {
            Text = $"{workforceType}:",
            CustomMinimumSize = new Vector2(100, 0)
        };
        headerRow.AddChild(typeLabel);

        var defaultLabel = new Label
        {
            Text = $"(Default: {defaultCount})",
            Modulate = new Color(0.7f, 0.7f, 0.7f)
        };
        headerRow.AddChild(defaultLabel);

        rowContainer.AddChild(headerRow);

        var configRow = new HBoxContainer();
        var configLabel = new Label
        {
            Text = "  Cost Config:",
            CustomMinimumSize = new Vector2(100, 0)
        };
        configRow.AddChild(configLabel);

        var configDropdown = new OptionButton
        {
            CustomMinimumSize = new Vector2(200, 0)
        };
        PopulateConfigDropdown(configDropdown, workforceType, configName);
        configRow.AddChild(configDropdown);

        rowContainer.AddChild(configRow);

        var countRow = new HBoxContainer();
        var countLabel = new Label
        {
            Text = "  Count Override:",
            CustomMinimumSize = new Vector2(100, 0)
        };
        countRow.AddChild(countLabel);

        var useOverrideCheck = new CheckBox
        {
            Text = "",
            ButtonPressed = overrideCount.HasValue
        };
        countRow.AddChild(useOverrideCheck);

        var countSpinBox = new SpinBox
        {
            MinValue = 0,
            MaxValue = 1000,
            Step = 1,
            Value = overrideCount ?? defaultCount,
            CustomMinimumSize = new Vector2(100, 0),
            Editable = overrideCount.HasValue
        };
        countRow.AddChild(countSpinBox);

        useOverrideCheck.Toggled += (toggled) =>
        {
            countSpinBox.Editable = toggled;
            if (!toggled)
            {
                countSpinBox.Value = defaultCount;
            }
        };

        rowContainer.AddChild(countRow);

        var separator = new HSeparator();
        rowContainer.AddChild(separator);

        _workforceRowsContainer?.AddChild(rowContainer);

        _workforceRows.Add(new WorkforceRowData
        {
            WorkforceType = workforceType,
            DefaultCount = defaultCount,
            ConfigDropdown = configDropdown,
            UseOverrideCheck = useOverrideCheck,
            CountSpinBox = countSpinBox,
            RowContainer = rowContainer
        });
    }

    private void PopulateConfigDropdown(OptionButton dropdown, WorkforceType workforceType, string? selectedConfigName)
    {
        dropdown.Clear();

        var configs = GetWorkforceConfigsForType(workforceType);

        if (configs.Count == 0)
        {
            dropdown.AddItem("(No configs available)", 0);
            dropdown.SetItemMetadata(0, "");
            dropdown.Disabled = true;
            return;
        }

        dropdown.Disabled = false;
        int selectedIndex = 0;

        for (int i = 0; i < configs.Count; i++)
        {
            var config = configs[i];
            dropdown.AddItem(config.Name, i);
            dropdown.SetItemMetadata(i, config.Name);

            if (config.Name == selectedConfigName)
            {
                selectedIndex = i;
            }
        }

        dropdown.Select(selectedIndex);
    }

    private List<WorkforceTypeConfig> GetWorkforceConfigsForType(WorkforceType workforceType)
    {
        var result = new List<WorkforceTypeConfig>();

        if (_sessionManager?.Session?.CurrentWorkspace?.WorkforceConfigs == null)
            return result;

        foreach (var config in _sessionManager.Session.CurrentWorkspace.WorkforceConfigs.Values)
        {
            if (config.WorkforceType == workforceType)
            {
                result.Add(config);
            }
        }

        return result;
    }

    public List<WorkforceRequirement>? GetWorkforceOverride()
    {
        var hasAnyOverride = _workforceRows.Any(r => r.UseOverrideCheck.ButtonPressed);

        if (!hasAnyOverride)
            return null;

        var overrides = new List<WorkforceRequirement>();

        foreach (var row in _workforceRows)
        {
            int count = row.UseOverrideCheck.ButtonPressed
                ? (int)row.CountSpinBox.Value
                : row.DefaultCount;

            overrides.Add(new WorkforceRequirement
            {
                WorkforceType = row.WorkforceType,
                Count = count
            });
        }

        return overrides;
    }

    public Dictionary<WorkforceType, string>? GetWorkforceConfigMapping()
    {
        var mapping = new Dictionary<WorkforceType, string>();

        foreach (var row in _workforceRows)
        {
            if (row.ConfigDropdown.Disabled || row.ConfigDropdown.Selected < 0)
                continue;

            var configName = row.ConfigDropdown.GetItemMetadata(row.ConfigDropdown.Selected).AsString();

            if (!string.IsNullOrEmpty(configName))
            {
                mapping[row.WorkforceType] = configName;
            }
        }

        return mapping.Count > 0 ? mapping : null;
    }

    private void ClearRows()
    {
        foreach (var row in _workforceRows)
        {
            row.RowContainer.QueueFree();
        }
        _workforceRows.Clear();
    }

    private class WorkforceRowData
    {
        public required WorkforceType WorkforceType { get; init; }
        public required int DefaultCount { get; init; }
        public required OptionButton ConfigDropdown { get; init; }
        public required CheckBox UseOverrideCheck { get; init; }
        public required SpinBox CountSpinBox { get; init; }
        public required VBoxContainer RowContainer { get; init; }
    }
}
