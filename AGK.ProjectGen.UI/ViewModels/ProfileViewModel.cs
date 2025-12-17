using System.Collections.ObjectModel;
using AGK.ProjectGen.Application.Interfaces;
using AGK.ProjectGen.Domain.Enums;
using AGK.ProjectGen.Domain.Schema;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AGK.ProjectGen.UI.ViewModels;

public partial class ProfileViewModel : ObservableObject
{
    private readonly IProfileRepository _repository;
    
    [ObservableProperty]
    private ObservableCollection<ProfileSchema> _profiles = new();
    
    [ObservableProperty]
    private ProfileSchema? _selectedProfile;

    [ObservableProperty]
    private StructureNodeDefinition? _selectedStructureNode;

    [ObservableProperty]
    private DictionarySchema? _selectedDictionary;

    [ObservableProperty]
    private DictionaryItem? _selectedDictionaryItem;

    public ProfileViewModel(IProfileRepository repository)
    {
        _repository = repository;
        LoadProfilesCommand.Execute(null);
    }

    [RelayCommand]
    private async Task LoadProfiles()
    {
        var list = await _repository.GetAllAsync();
        
        if (!list.Any())
        {
            // Создаём демо профиль с базовыми словарями
            var demo = CreateDemoProfile();
            await _repository.SaveAsync(demo);
            list.Add(demo);
        }

        Profiles = new ObservableCollection<ProfileSchema>(list);
    }

    private ProfileSchema CreateDemoProfile()
    {
        var profile = new ProfileSchema 
        { 
            Name = "Стандартный профиль",
            Version = "1.0"
        };
        
        // Атрибуты проекта (поля ввода при создании)
        profile.ProjectAttributes.Add(new FieldSchema 
        { 
            Key = "ProjectCode", 
            DisplayName = "Шифр проекта", 
            Type = AttributeType.String, 
            IsRequired = true,
            Description = "Уникальный шифр проекта",
            Order = 0
        });
        profile.ProjectAttributes.Add(new FieldSchema 
        { 
            Key = "ProjectName", 
            DisplayName = "Название проекта", 
            Type = AttributeType.String, 
            IsRequired = true,
            Description = "Полное название проекта",
            Order = 1
        });
        profile.ProjectAttributes.Add(new FieldSchema 
        { 
            Key = "ProjectShortName", 
            DisplayName = "Краткое название", 
            Type = AttributeType.String, 
            IsRequired = false,
            Description = "Сокращённое название для папок",
            Order = 2
        });
        profile.ProjectAttributes.Add(new FieldSchema 
        { 
            Key = "Client", 
            DisplayName = "Заказчик", 
            Type = AttributeType.String, 
            IsRequired = false,
            Order = 3
        });
        
        // Словарь стадий
        profile.Dictionaries.Add(new DictionarySchema
        {
            Key = "Stages",
            DisplayName = "Стадии проектирования",
            Items = new ObservableCollection<DictionaryItem>
            {
                new() { Code = "П", Name = "Проектная документация" },
                new() { Code = "Р", Name = "Рабочая документация" }
            }
        });
        
        // Словарь очередей
        profile.Dictionaries.Add(new DictionarySchema
        {
            Key = "Queues",
            DisplayName = "Очереди строительства",
            Items = new ObservableCollection<DictionaryItem>
            {
                new() { Code = "1", Name = "1-я очередь" },
                new() { Code = "2", Name = "2-я очередь" }
            }
        });
        
        // Словарь разделов
        profile.Dictionaries.Add(new DictionarySchema
        {
            Key = "Disciplines",
            DisplayName = "Разделы документации",
            Items = new ObservableCollection<DictionaryItem>
            {
                new() { Code = "АР", Name = "Архитектурные решения" },
                new() { Code = "КР", Name = "Конструктивные решения" },
                new() { Code = "ОВ", Name = "Отопление и вентиляция" },
                new() { Code = "ВК", Name = "Водоснабжение и канализация" },
                new() { Code = "ЭО", Name = "Электрооборудование" }
            }
        });
        
        // Словарь статусов (служебные папки)
        profile.Dictionaries.Add(new DictionarySchema
        {
            Key = "SystemFolders",
            DisplayName = "Статусы (служебные папки)",
            Items = new ObservableCollection<DictionaryItem>
            {
                new() { Code = "Work", Name = "Рабочие материалы" },
                new() { Code = "Publish", Name = "Выпуск" },
                new() { Code = "Archive", Name = "Архив" }
            }
        });
        
        // Динамический словарь — Позиции по генплану (заполняется при создании проекта)
        profile.Dictionaries.Add(new DictionarySchema
        {
            Key = "Buildings",
            DisplayName = "Позиции по генплану",
            IsDynamic = true  // Заполняется при создании проекта!
        });
        
        // Типы узлов (формулы используют ключи атрибутов и словарей)
        profile.NodeTypes.Add(new NodeTypeSchema { TypeId = "ProjectRoot", DisplayName = "Корень проекта", DefaultFormula = "{ProjectCode}_{ProjectShortName}" });
        profile.NodeTypes.Add(new NodeTypeSchema { TypeId = "BuildingFolder", DisplayName = "Папка позиции", DefaultFormula = "{Buildings.Code}_{Buildings.Name}" });
        profile.NodeTypes.Add(new NodeTypeSchema { TypeId = "StageFolder", DisplayName = "Папка стадии", DefaultFormula = "{Stages.Code}_{Stages.Name}" });
        profile.NodeTypes.Add(new NodeTypeSchema { TypeId = "QueueFolder", DisplayName = "Папка очереди", DefaultFormula = "{Queues.Code}_Очередь" });
        profile.NodeTypes.Add(new NodeTypeSchema { TypeId = "DisciplineFolder", DisplayName = "Папка раздела", DefaultFormula = "{ProjectCode}_{Buildings.Code}_{Disciplines.Code}" });
        profile.NodeTypes.Add(new NodeTypeSchema { TypeId = "SystemFolder", DisplayName = "Служебная папка", DefaultFormula = "{SystemFolders.Name}" });
        
        // Структура: Позиции → Стадии → Разделы
        var buildingNode = new StructureNodeDefinition 
        { 
            NodeTypeId = "BuildingFolder", 
            Multiplicity = MultiplicitySource.Dictionary,
            SourceKey = "Buildings"  // Динамический — заполняется при создании
        };
        
        var stageNode = new StructureNodeDefinition 
        { 
            NodeTypeId = "StageFolder", 
            Multiplicity = MultiplicitySource.Dictionary,
            SourceKey = "Stages"
        };
        
        var disciplineNode = new StructureNodeDefinition
        {
            NodeTypeId = "DisciplineFolder",
            Multiplicity = MultiplicitySource.Dictionary,
            SourceKey = "Disciplines"
        };
        
        stageNode.Children.Add(disciplineNode);
        buildingNode.Children.Add(stageNode);
        profile.Structure.RootNodes.Add(buildingNode);
        
        return profile;
    }

    #region Profile Commands

    [RelayCommand]
    private void CreateProfile()
    {
        var newProfile = CreateDemoProfile();
        newProfile.Name = "Новый профиль";
        Profiles.Add(newProfile);
        SelectedProfile = newProfile;
    }

    [RelayCommand]
    private async Task SaveProfile()
    {
        if (SelectedProfile != null)
        {
            await _repository.SaveAsync(SelectedProfile);
        }
    }

    #endregion

    #region Attribute Commands

    [ObservableProperty]
    private FieldSchema? _selectedAttribute;

    [RelayCommand]
    private void AddAttribute()
    {
        if (SelectedProfile == null) return;
        
        var newAttr = new FieldSchema
        {
            Key = $"Attr{SelectedProfile.ProjectAttributes.Count + 1}",
            DisplayName = "Новый атрибут",
            Type = AttributeType.String,
            Order = SelectedProfile.ProjectAttributes.Count
        };
        SelectedProfile.ProjectAttributes.Add(newAttr);
        SelectedAttribute = newAttr;
    }

    [RelayCommand]
    private void RemoveAttribute()
    {
        if (SelectedProfile == null || SelectedAttribute == null) return;
        SelectedProfile.ProjectAttributes.Remove(SelectedAttribute);
        SelectedAttribute = null;
    }

    #endregion

    #region Dictionary Commands

    [RelayCommand]
    private void AddDictionary()
    {
        if (SelectedProfile == null) return;
        
        var newDict = new DictionarySchema
        {
            Key = $"NewDict{SelectedProfile.Dictionaries.Count + 1}",
            DisplayName = "Новый словарь"
        };
        SelectedProfile.Dictionaries.Add(newDict);
        SelectedDictionary = newDict;
    }

    [RelayCommand]
    private void RemoveDictionary()
    {
        if (SelectedProfile == null || SelectedDictionary == null) return;
        SelectedProfile.Dictionaries.Remove(SelectedDictionary);
        SelectedDictionary = null;
    }

    [RelayCommand]
    private void AddDictionaryItem()
    {
        if (SelectedDictionary == null) return;
        
        var newItem = new DictionaryItem
        {
            Code = $"NEW{SelectedDictionary.Items.Count + 1}",
            Name = "Новый элемент"
        };
        SelectedDictionary.Items.Add(newItem);
        SelectedDictionaryItem = newItem;
    }

    [RelayCommand]
    private void RemoveDictionaryItem()
    {
        if (SelectedDictionary == null || SelectedDictionaryItem == null) return;
        SelectedDictionary.Items.Remove(SelectedDictionaryItem);
        SelectedDictionaryItem = null;
    }

    #endregion

    #region NodeType Commands

    [RelayCommand]
    private void AddNodeType()
    {
        if (SelectedProfile == null) return;
        SelectedProfile.NodeTypes.Add(new NodeTypeSchema 
        { 
            TypeId = $"Type{SelectedProfile.NodeTypes.Count + 1}", 
            DisplayName = "Новый тип" 
        });
    }

    [RelayCommand]
    private void RemoveNodeType()
    {
        if (SelectedProfile == null) return;
        var last = SelectedProfile.NodeTypes.LastOrDefault();
        if (last != null)
            SelectedProfile.NodeTypes.Remove(last);
    }

    #endregion

    #region Structure Commands

    [RelayCommand]
    private void AddRootNode()
    {
        if (SelectedProfile == null) return;
        SelectedProfile.Structure.RootNodes.Add(new StructureNodeDefinition 
        { 
            NodeTypeId = "Folder", 
            NamingFormulaOverride = "New Folder" 
        });
    }
    
    [RelayCommand]
    private void AddChildNode()
    {
        if (SelectedStructureNode == null) return;
        SelectedStructureNode.Children.Add(new StructureNodeDefinition 
        { 
            NodeTypeId = "Folder", 
            NamingFormulaOverride = "New Child" 
        });
    }
    
    [RelayCommand]
    private void RemoveNode()
    {
        if (SelectedStructureNode == null || SelectedProfile == null) return;
        if (RemoveNodeRecursive(SelectedProfile.Structure.RootNodes, SelectedStructureNode))
        {
            SelectedStructureNode = null;
        }
    }

    private bool RemoveNodeRecursive(ObservableCollection<StructureNodeDefinition> collection, StructureNodeDefinition nodeToRemove)
    {
        if (collection.Contains(nodeToRemove))
        {
            collection.Remove(nodeToRemove);
            return true;
        }
        foreach (var node in collection)
        {
            if (RemoveNodeRecursive(node.Children, nodeToRemove)) return true;
        }
        return false;
    }

    #endregion
}
