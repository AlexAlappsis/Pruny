using Godot;

namespace Pruny.UI.Components;

public partial class RecipeSelector : HBoxContainer
{
    [Signal]
    public delegate void RecipeSelectedEventHandler(string recipeId);

    private SessionManager? _sessionManager;
    private OptionButton? _recipeDropdown;
    private Label? _buildingLabel;

    private string _selectedRecipeId = "";

    public override void _Ready()
    {
        _sessionManager = GetNode<SessionManager>("/root/SessionManager");

        _recipeDropdown = GetNode<OptionButton>("RecipeDropdown");
        _buildingLabel = GetNode<Label>("BuildingLabel");

        PopulateRecipes();

        _recipeDropdown.ItemSelected += OnRecipeSelected;
    }

    private void PopulateRecipes()
    {
        if (_recipeDropdown == null || _sessionManager?.Session?.GameData == null)
            return;

        _recipeDropdown.Clear();

        var recipes = _sessionManager.Session.GameData.Recipes;

        if (recipes.Count == 0)
        {
            _recipeDropdown.AddItem("(No recipes)", 0);
            _recipeDropdown.Disabled = true;
            return;
        }

        _recipeDropdown.Disabled = false;
        var sortedRecipes = recipes.OrderBy(r => r.Key).ToList();

        for (int i = 0; i < sortedRecipes.Count; i++)
        {
            var recipe = sortedRecipes[i];
            _recipeDropdown.AddItem(recipe.Key, i);
            _recipeDropdown.SetItemMetadata(i, recipe.Key);
        }

        if (sortedRecipes.Count > 0)
        {
            _recipeDropdown.Select(0);
            OnRecipeSelected(0);
        }
    }

    private void OnRecipeSelected(long index)
    {
        if (_recipeDropdown == null || _sessionManager?.Session?.GameData == null)
            return;

        _selectedRecipeId = _recipeDropdown.GetItemMetadata((int)index).AsString();

        if (_sessionManager.Session.GameData.Recipes.TryGetValue(_selectedRecipeId, out var recipe))
        {
            if (_sessionManager.Session.GameData.Buildings.TryGetValue(recipe.BuildingId, out var building))
            {
                _buildingLabel!.Text = $"Building: {building.Name}";
            }
            else
            {
                _buildingLabel!.Text = $"Building: {recipe.BuildingId}";
            }
        }

        EmitSignal(SignalName.RecipeSelected, _selectedRecipeId);
    }

    public void SelectRecipe(string recipeId)
    {
        if (_recipeDropdown == null)
            return;

        for (int i = 0; i < _recipeDropdown.ItemCount; i++)
        {
            if (_recipeDropdown.GetItemMetadata(i).AsString() == recipeId)
            {
                _recipeDropdown.Select(i);
                OnRecipeSelected(i);
                return;
            }
        }
    }

    public string GetSelectedRecipeId()
    {
        return _selectedRecipeId;
    }

    public override void _ExitTree()
    {
        if (_recipeDropdown != null)
            _recipeDropdown.ItemSelected -= OnRecipeSelected;
    }
}
