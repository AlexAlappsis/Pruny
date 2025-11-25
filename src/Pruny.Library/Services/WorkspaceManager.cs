namespace Pruny.Library.Services;

using System.Text.Json;
using System.Text.Json.Serialization;
using Pruny.Library.Events;
using Pruny.Library.Models;

public class WorkspaceManager
{
    private readonly JsonSerializerOptions _serializerOptions;
    private Workspace? _currentWorkspace;

    public WorkspaceManager(JsonSerializerOptions? serializerOptions = null)
    {
        _serializerOptions = serializerOptions ?? new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };
    }

    public event EventHandler<WorkspaceModifiedEventArgs>? WorkspaceModified;

    public Workspace? CurrentWorkspace => _currentWorkspace;

    public bool IsDirty { get; private set; }

    public Workspace CreateNewWorkspace(string name, string? id = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Workspace name is required.", nameof(name));

        var now = DateTimeOffset.UtcNow;
        var workspace = new Workspace
        {
            Version = 1,
            Id = string.IsNullOrWhiteSpace(id) ? Guid.NewGuid().ToString("N") : id,
            Name = name,
            CreatedAt = now,
            LastModifiedAt = now,
            MarketDataFetchedAt = null,
            ProductionLines = new(),
            CustomPrices = new(),
            WorkforceConfig = null,
        };

        _currentWorkspace = workspace;
        MarkDirty("Workspace created");

        return workspace;
    }

    public Workspace LoadWorkspace(string workspaceJson)
    {
        if (string.IsNullOrWhiteSpace(workspaceJson))
            throw new ArgumentException("Workspace JSON must not be empty.", nameof(workspaceJson));

        Workspace workspace;

        try
        {
            using var document = JsonDocument.Parse(workspaceJson);
            var hasVersion = document.RootElement.ValueKind == JsonValueKind.Object &&
                             document.RootElement.EnumerateObject()
                                 .Any(p => string.Equals(p.Name, "version", StringComparison.OrdinalIgnoreCase));

            if (!hasVersion)
                throw new InvalidDataException("Workspace JSON is missing required 'version' field.");

            workspace = document.RootElement.Deserialize<Workspace>(_serializerOptions)
                ?? throw new InvalidDataException("Workspace JSON could not be deserialized.");
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException("Workspace JSON is invalid.", ex);
        }

        ValidateWorkspace(workspace);
        EnsureTimestamps(workspace);

        _currentWorkspace = workspace;
        IsDirty = false;

        return workspace;
    }

    public void SetWorkspace(Workspace workspace)
    {
        ValidateWorkspace(workspace);
        EnsureTimestamps(workspace);
        _currentWorkspace = workspace;
        IsDirty = false;
    }

    public string SaveWorkspaceToJson()
    {
        var workspace = EnsureWorkspaceLoaded();
        workspace.LastModifiedAt = DateTimeOffset.UtcNow;

        var json = JsonSerializer.Serialize(workspace, _serializerOptions);
        IsDirty = false;

        return json;
    }

    public void ApplyChanges(Action<Workspace> updateAction, string reason)
    {
        if (updateAction is null)
            throw new ArgumentNullException(nameof(updateAction));

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Reason must be provided.", nameof(reason));

        var workspace = EnsureWorkspaceLoaded();
        updateAction(workspace);

        MarkDirty(reason);
    }

    public void MarkDirty(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Reason must be provided.", nameof(reason));

        var workspace = EnsureWorkspaceLoaded();
        workspace.LastModifiedAt = DateTimeOffset.UtcNow;
        IsDirty = true;

        WorkspaceModified?.Invoke(this, new WorkspaceModifiedEventArgs
        {
            WorkspaceId = workspace.Id,
            Reason = reason,
        });
    }

    private Workspace EnsureWorkspaceLoaded()
    {
        return _currentWorkspace ?? throw new InvalidOperationException("No workspace is loaded.");
    }

    private static void ValidateWorkspace(Workspace workspace)
    {
        if (workspace.Version <= 0)
            throw new InvalidDataException("Workspace version is missing or invalid.");

        if (string.IsNullOrWhiteSpace(workspace.Id))
            throw new InvalidDataException("Workspace id is required.");

        if (string.IsNullOrWhiteSpace(workspace.Name))
            throw new InvalidDataException("Workspace name is required.");
    }

    private static void EnsureTimestamps(Workspace workspace)
    {
        if (workspace.CreatedAt == default)
            workspace.CreatedAt = DateTimeOffset.UtcNow;

        if (workspace.LastModifiedAt == default)
            workspace.LastModifiedAt = workspace.CreatedAt;
    }
}
