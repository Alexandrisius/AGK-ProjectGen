using System.Collections.ObjectModel;

namespace AGK.ProjectGen.Domain.Schema;

public class DictionarySchema
{
    /// <summary>
    /// Уникальный ключ словаря (используется в формулах, например {Stages.Code}).
    /// </summary>
    public string Key { get; set; } = string.Empty;
    
    /// <summary>
    /// Отображаемое название словаря.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
    
    /// <summary>
    /// Если true — словарь заполняется при создании проекта (как Table).
    /// Если false — элементы предзаполнены в профиле.
    /// </summary>
    public bool IsDynamic { get; set; } = false;
    
    /// <summary>
    /// Элементы словаря. Для IsDynamic=true будет пустым в профиле.
    /// </summary>
    public ObservableCollection<DictionaryItem> Items { get; set; } = new();
}

public class DictionaryItem
{
    public string Code { get; set; } = string.Empty; // "P", "R", "5.1"
    public string Name { get; set; } = string.Empty; // "Стадия П", "Здание 5"
    public Dictionary<string, string> Metadata { get; set; } = new();
}
