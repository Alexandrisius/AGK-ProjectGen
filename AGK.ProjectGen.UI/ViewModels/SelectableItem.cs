using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AGK.ProjectGen.UI.ViewModels;

/// <summary>
/// Элемент списка с галочкой для выбора
/// </summary>
public partial class SelectableItem : ObservableObject
{
    [ObservableProperty]
    private bool _isSelected;
    
    [ObservableProperty]
    private string _code = string.Empty;
    
    [ObservableProperty]
    private string _name = string.Empty;
    
    [ObservableProperty]
    private string _displayText = string.Empty;
    
    [ObservableProperty]
    private string? _aclTemplateId;
    
    public SelectableItem() { }
    
    public SelectableItem(string code, string name, bool isSelected = false)
    {
        Code = code;
        Name = name;
        IsSelected = isSelected;
        DisplayText = $"{code} — {name}";
    }
}

/// <summary>
/// Группа выбираемых элементов (например "Стадии", "Разделы")
/// </summary>
public partial class SelectionGroup : ObservableObject
{
    [ObservableProperty]
    private string _key = string.Empty;
    
    [ObservableProperty]
    private string _displayName = string.Empty;
    
    [ObservableProperty]
    private ObservableCollection<SelectableItem> _items = new();
    
    public IEnumerable<SelectableItem> SelectedItems => Items.Where(i => i.IsSelected);
    
    public IEnumerable<string> SelectedCodes => SelectedItems.Select(i => i.Code);
    
    public void SelectAll()
    {
        foreach (var item in Items)
            item.IsSelected = true;
    }
    
    public void DeselectAll()
    {
        foreach (var item in Items)
            item.IsSelected = false;
    }
    
    public void SetSelection(IEnumerable<string> codes)
    {
        var codeSet = codes.ToHashSet();
        foreach (var item in Items)
            item.IsSelected = codeSet.Contains(item.Code);
    }
}

/// <summary>
/// Значение атрибута проекта. Связывает определение атрибута (FieldSchema) 
/// с фактическим введённым значением.
/// </summary>
public partial class AttributeValueItem : ObservableObject
{
    /// <summary>
    /// Ключ атрибута (из FieldSchema.Key).
    /// </summary>
    [ObservableProperty]
    private string _key = string.Empty;
    
    /// <summary>
    /// Название для отображения.
    /// </summary>
    [ObservableProperty]
    private string _displayName = string.Empty;
    
    /// <summary>
    /// Тип атрибута (для выбора элемента UI).
    /// </summary>
    [ObservableProperty]
    private string _attributeType = "String";
    
    /// <summary>
    /// Обязательный ли атрибут.
    /// </summary>
    [ObservableProperty]
    private bool _isRequired;
    
    /// <summary>
    /// Значение атрибута (string для Text, Select; List для MultiSelect).
    /// </summary>
    [ObservableProperty]
    private string _value = string.Empty;
    
    /// <summary>
    /// Подсказка/описание.
    /// </summary>
    [ObservableProperty]
    private string? _description;
    
    /// <summary>
    /// Ключ связанного словаря (для Select/MultiSelect).
    /// </summary>
    [ObservableProperty]
    private string? _dictionaryKey;
    
    /// <summary>
    /// Элементы для выбора (если тип Select/MultiSelect).
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<SelectableItem> _selectItems = new();
}
