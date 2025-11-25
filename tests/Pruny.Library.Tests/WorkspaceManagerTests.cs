using FluentAssertions;
using Pruny.Library.Events;
using Pruny.Library.Models;
using Pruny.Library.Services;

namespace Pruny.Library.Tests;

public class WorkspaceManagerTests
{
    [Fact]
    public void CreateNewWorkspace_SetsDefaults_And_MarksDirty_And_RaisesEvent()
    {
        var manager = new WorkspaceManager();
        WorkspaceModifiedEventArgs? eventArgs = null;
        manager.WorkspaceModified += (_, args) => eventArgs = args;

        var workspace = manager.CreateNewWorkspace("Test Workspace", "custom-id");

        workspace.Id.Should().Be("custom-id");
        workspace.Name.Should().Be("Test Workspace");
        workspace.Version.Should().Be(1);
        workspace.CustomPrices.Should().BeEmpty();
        workspace.ProductionLines.Should().BeEmpty();
        workspace.CreatedAt.Should().NotBe(default);
        workspace.LastModifiedAt.Should().NotBe(default);

        manager.CurrentWorkspace.Should().BeSameAs(workspace);
        manager.IsDirty.Should().BeTrue();

        eventArgs.Should().NotBeNull();
        eventArgs!.WorkspaceId.Should().Be("custom-id");
        eventArgs.Reason.Should().Be("Workspace created");
    }

    [Fact]
    public void LoadWorkspace_Deserializes_And_ClearsDirty()
    {
        var now = DateTimeOffset.Parse("2024-01-01T00:00:00Z");
        var json = """
        {
          "version": 1,
          "id": "ws-1",
          "name": "Loaded",
          "createdAt": "2024-01-01T00:00:00Z",
          "lastModifiedAt": "2024-01-01T00:00:00Z",
          "productionLines": [],
          "customPrices": {}
        }
        """;
        var manager = new WorkspaceManager();

        var workspace = manager.LoadWorkspace(json);

        workspace.Id.Should().Be("ws-1");
        workspace.Name.Should().Be("Loaded");
        workspace.CreatedAt.Should().Be(now);
        workspace.LastModifiedAt.Should().Be(now);
        manager.IsDirty.Should().BeFalse();
        manager.CurrentWorkspace.Should().NotBeNull();
        manager.CurrentWorkspace!.Id.Should().Be("ws-1");
    }

    [Fact]
    public void LoadWorkspace_MissingVersionThrows()
    {
        var json = """
        {
          "id": "ws-1",
          "name": "Loaded",
          "createdAt": "2024-01-01T00:00:00Z",
          "lastModifiedAt": "2024-01-01T00:00:00Z",
          "productionLines": [],
          "customPrices": {}
        }
        """;
        var manager = new WorkspaceManager();

        var act = () => manager.LoadWorkspace(json);

        act.Should().Throw<InvalidDataException>()
            .WithMessage("*missing required 'version'*");
    }

    [Fact]
    public void ApplyChanges_MarksDirty_And_RaisesEvent()
    {
        var manager = CreateManagerWithWorkspace();
        var initialModifiedAt = manager.CurrentWorkspace!.LastModifiedAt;
        WorkspaceModifiedEventArgs? eventArgs = null;
        manager.WorkspaceModified += (_, args) => eventArgs = args;

        manager.ApplyChanges(ws => ws.Name = "Renamed", "Renamed workspace");

        manager.IsDirty.Should().BeTrue();
        manager.CurrentWorkspace!.Name.Should().Be("Renamed");
        manager.CurrentWorkspace.LastModifiedAt.Should().BeAfter(initialModifiedAt);
        eventArgs.Should().NotBeNull();
        eventArgs!.WorkspaceId.Should().Be(manager.CurrentWorkspace.Id);
        eventArgs.Reason.Should().Be("Renamed workspace");
    }

    [Fact]
    public void SaveWorkspaceToJson_UpdatesTimestamp_And_ClearsDirty()
    {
        var manager = CreateManagerWithWorkspace();
        manager.MarkDirty("test");
        var modifiedBefore = manager.CurrentWorkspace!.LastModifiedAt;

        var json = manager.SaveWorkspaceToJson();

        manager.IsDirty.Should().BeFalse();
        manager.CurrentWorkspace!.LastModifiedAt.Should().BeAfter(modifiedBefore);
        json.Should().Contain("\"id\": \"ws-123\"");
        json.Should().Contain("\"name\": \"Existing\"");
    }

    [Fact]
    public void MarkDirty_WithoutWorkspaceThrows()
    {
        var manager = new WorkspaceManager();

        var act = () => manager.MarkDirty("no workspace");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*No workspace is loaded*");
    }

    private static WorkspaceManager CreateManagerWithWorkspace()
    {
        var manager = new WorkspaceManager();
        var workspace = new Workspace
        {
            Version = 1,
            Id = "ws-123",
            Name = "Existing",
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-10),
            LastModifiedAt = DateTimeOffset.UtcNow.AddMinutes(-10),
            ProductionLines = new(),
            CustomPrices = new(),
        };

        manager.SetWorkspace(workspace);
        return manager;
    }
}
