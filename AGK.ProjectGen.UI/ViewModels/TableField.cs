using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AGK.ProjectGen.UI.ViewModels;

/// <summary>
/// Строка таблицы, которую вводит ГИП (например здание по генплану)
/// </summary>
public partial class TableRowItem : ObservableObject
{
    [ObservableProperty]
    private string _code = string.Empty;
    
    [ObservableProperty]
    private string _name = string.Empty;
    
    [ObservableProperty]
    private string _description = string.Empty;
    
    public TableRowItem() { }
    
    public TableRowItem(string code, string name)
    {
        Code = code;
        Name = name;
    }
}

/// <summary>
/// Табличное поле для ввода списка (здания, позиции генплана и т.д.)
/// </summary>
public partial class TableField : ObservableObject
{
    [ObservableProperty]
    private string _key = string.Empty; // "Buildings"
    
    [ObservableProperty]
    private string _displayName = string.Empty; // "Здания по генплану"
    
    [ObservableProperty]
    private ObservableCollection<TableRowItem> _rows = new();
    
    public void AddRow()
    {
        Rows.Add(new TableRowItem
        {
            Code = $"{Rows.Count + 1}",
            Name = "Новый элемент"
        });
    }
    
    public void RemoveRow(TableRowItem? item)
    {
        if (item != null && Rows.Contains(item))
            Rows.Remove(item);
    }
}
