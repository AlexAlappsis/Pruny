using Godot;

namespace Pruny.UI.Components;

public partial class SearchableDropdown : VBoxContainer
{
    public event Action<string>? ItemSelected;

    private LineEdit? _searchInput;
    private Button? _dropdownButton;
    private PopupPanel? _popup;
    private ItemList? _itemList;

    private List<DropdownItem> _allItems = new();
    private string _selectedValue = "";
    private bool _isUserTyping = false;

    public override void _Ready()
    {
        _searchInput = GetNode<LineEdit>("SearchInput");
        _dropdownButton = GetNode<Button>("SearchInput/DropdownButton");
        _popup = GetNode<PopupPanel>("Popup");
        _itemList = GetNode<ItemList>("Popup/ItemList");

        _searchInput.TextChanged += OnSearchTextChanged;
        _searchInput.TextSubmitted += OnSearchTextSubmitted;
        _searchInput.FocusEntered += OnSearchFocusEntered;
        _dropdownButton.Pressed += OnDropdownButtonPressed;
        _itemList.ItemSelected += OnItemListItemSelected;
        _itemList.ItemActivated += OnItemListItemActivated;

        _popup.PopupHide += OnPopupHide;
    }

    public void SetItems(List<DropdownItem> items)
    {
        _allItems = items;
        RebuildItemList("");
    }

    public void SetSelectedValue(string value)
    {
        _selectedValue = value;
        UpdateSearchInputText();
    }

    public string GetSelectedValue()
    {
        return _selectedValue;
    }

    public DropdownItem? GetSelectedItem()
    {
        return _allItems.FirstOrDefault(item => item.Value == _selectedValue);
    }

    private void OnSearchTextChanged(string newText)
    {
        _isUserTyping = true;

        if (_popup!.Visible)
        {
            RebuildItemList(newText);
        }
    }

    private void OnSearchTextSubmitted(string text)
    {
        if (!_popup!.Visible)
        {
            RebuildItemList(text);
            ShowPopup();
        }
        else if (_itemList!.ItemCount > 0)
        {
            var selectedIndex = _itemList.GetSelectedItems().Length > 0
                ? _itemList.GetSelectedItems()[0]
                : 0;
            SelectItemAtIndex(selectedIndex);
            _isUserTyping = false;
            HidePopup();
        }
    }

    private void OnSearchFocusEntered()
    {
        if (!_isUserTyping)
        {
            _searchInput!.SelectAll();
        }
    }

    private void OnDropdownButtonPressed()
    {
        if (_popup!.Visible)
        {
            HidePopup();
        }
        else
        {
            _isUserTyping = false;
            _searchInput!.Text = "";
            RebuildItemList("");
            ShowPopup();
            _searchInput.GrabFocus();
        }
    }

    private void OnItemListItemSelected(long index)
    {
        // Preview selection (could show tooltip or preview panel)
    }

    private void OnItemListItemActivated(long index)
    {
        SelectItemAtIndex((int)index);
        HidePopup();
    }

    private void OnPopupHide()
    {
        if (!_isUserTyping)
        {
            UpdateSearchInputText();
        }
    }

    private void RebuildItemList(string filter)
    {
        if (_itemList == null) return;

        _itemList.Clear();

        var filteredItems = string.IsNullOrWhiteSpace(filter)
            ? _allItems
            : _allItems.Where(item =>
                item.DisplayText.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                item.Value.Contains(filter, StringComparison.OrdinalIgnoreCase))
              .OrderBy(item => item.DisplayText.IndexOf(filter, StringComparison.OrdinalIgnoreCase))
              .ToList();

        for (int i = 0; i < filteredItems.Count; i++)
        {
            var item = filteredItems[i];
            _itemList.AddItem(item.DisplayText);
            _itemList.SetItemMetadata(i, item.Value);

            if (item.Value == _selectedValue)
            {
                _itemList.Select(i);
            }
        }

        if (filteredItems.Count > 0 && _itemList.GetSelectedItems().Length == 0)
        {
            _itemList.Select(0);
        }
    }

    private void SelectItemAtIndex(int index)
    {
        if (_itemList == null || index < 0 || index >= _itemList.ItemCount)
            return;

        _selectedValue = _itemList.GetItemMetadata(index).AsString();
        _isUserTyping = false;
        UpdateSearchInputText();
        ItemSelected?.Invoke(_selectedValue);
    }

    private void UpdateSearchInputText()
    {
        if (_searchInput == null) return;

        var selectedItem = _allItems.FirstOrDefault(item => item.Value == _selectedValue);
        _isUserTyping = false;
        _searchInput.Text = selectedItem?.DisplayText ?? "";
    }

    private void ShowPopup()
    {
        if (_popup == null || _searchInput == null) return;

        var inputGlobalPos = _searchInput.GlobalPosition;
        var inputSize = _searchInput.Size;

        _popup.Position = new Vector2I(
            (int)inputGlobalPos.X,
            (int)(inputGlobalPos.Y + inputSize.Y)
        );

        _popup.Size = new Vector2I((int)inputSize.X, 200);

        if (!_popup.Visible)
        {
            _popup.Popup();
            CallDeferred(MethodName.RestoreFocus);
        }
    }

    private void RestoreFocus()
    {
        if (_searchInput != null && _searchInput.HasFocus() == false)
        {
            _searchInput.GrabFocus();
            _searchInput.CaretColumn = _searchInput.Text.Length;
        }
    }

    private void HidePopup()
    {
        if (_popup != null && _popup.Visible)
        {
            _popup.Hide();
        }
    }

    public override void _ExitTree()
    {
        if (_searchInput != null)
        {
            _searchInput.TextChanged -= OnSearchTextChanged;
            _searchInput.TextSubmitted -= OnSearchTextSubmitted;
            _searchInput.FocusEntered -= OnSearchFocusEntered;
        }
        if (_dropdownButton != null)
            _dropdownButton.Pressed -= OnDropdownButtonPressed;
        if (_itemList != null)
        {
            _itemList.ItemSelected -= OnItemListItemSelected;
            _itemList.ItemActivated -= OnItemListItemActivated;
        }
        if (_popup != null)
            _popup.PopupHide -= OnPopupHide;
    }
}

public class DropdownItem
{
    public required string Value { get; init; }
    public required string DisplayText { get; init; }
}
