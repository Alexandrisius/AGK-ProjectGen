using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.IO;
using AGK.ProjectGen.Application.Interfaces;
using AGK.ProjectGen.Domain.Entities;
using AGK.ProjectGen.Domain.Schema;
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;
using System.Windows;
using AGK.ProjectGen.UI.Views;

namespace AGK.ProjectGen.UI.ViewModels;

public partial class ProjectViewModel : ObservableObject
{
    private readonly IProfileRepository _profileRepository;
    private readonly IProjectManagerService _projectManagerService;
    private readonly INamingEngine _namingEngine;
    private readonly IAclService _aclService;

    #region Основные свойства

    [ObservableProperty]
    private ObservableCollection<ProfileSchema> _profiles = new();

    [ObservableProperty]
    private ProfileSchema? _selectedProfile;

    /// <summary>
    /// Путь для создания проекта (не меняется от профиля).
    /// </summary>
    [ObservableProperty]
    private string _projectPath = @"C:\Projects";

    /// <summary>
    /// Динамическая коллекция атрибутов проекта (заполняется из профиля).
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<AttributeValueItem> _dynamicAttributes = new();

    [ObservableProperty]
    private ObservableCollection<GeneratedNode> _previewStructure = new();

    [ObservableProperty]
    private bool _isPreviewGenerated;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private GeneratedNode? _selectedNode;

    #region Режим редактирования

    /// <summary>
    /// Режим редактирования (true) или создания нового проекта (false).
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ViewTitle))]
    [NotifyPropertyChangedFor(nameof(SaveButtonText))]
    private bool _isEditMode;

    /// <summary>
    /// Текущий редактируемый проект (null для создания нового).
    /// </summary>
    [ObservableProperty]
    private Project? _currentProject;

    /// <summary>
    /// Заголовок для отображения в UI.
    /// </summary>
    public string ViewTitle => IsEditMode ? "Редактирование проекта" : "Создание нового проекта";
    
    /// <summary>
    /// Текст кнопки сохранения.
    /// </summary>
    public string SaveButtonText => IsEditMode ? "🔄  Обновить проект" : "🚀  Создать проект";

    #endregion

    /// <summary>
    /// Получает значение атрибута по ключу из динамических атрибутов.
    /// </summary>
    public string GetAttributeValue(string key)
    {
        return DynamicAttributes.FirstOrDefault(a => a.Key == key)?.Value ?? string.Empty;
    }
    
    // Обратная совместимость для генерации структуры
    public string ProjectCode => GetAttributeValue("ProjectCode");
    public string ProjectName => GetAttributeValue("ProjectName");
    public string ProjectShortName => GetAttributeValue("ProjectShortName");

    #endregion

    #region Группы выбора (галочки) — динамически на основе профиля

    /// <summary>
    /// Динамическая коллекция групп выбора. Заполняется только теми словарями, 
    /// которые реально используются в структуре профиля (SourceKey в StructureNodeDefinition).
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<SelectionGroup> _dynamicSelectionGroups = new();

    #endregion

    #region Табличные поля (вводит ГИП) — динамически на основе профиля

    /// <summary>
    /// Динамическая коллекция табличных полей. Заполняется только теми источниками,
    /// которые имеют Multiplicity = Table в структуре профиля.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<TableField> _dynamicTableFields = new();

    [ObservableProperty]
    private TableRowItem? _selectedTableRow;

    #endregion

    public ProjectViewModel(
        IProfileRepository profileRepository, 
        IProjectManagerService projectManagerService,
        INamingEngine namingEngine,
        IAclService aclService)
    {
        _profileRepository = profileRepository;
        _projectManagerService = projectManagerService;
        _namingEngine = namingEngine;
        _aclService = aclService;

        LoadProfilesCommand.Execute(null);
    }

    partial void OnSelectedProfileChanged(ProfileSchema? value)
    {
        if (value != null)
        {
            LoadProfileSelections(value);
        }
    }

    /// <summary>
    /// Проект ожидающий восстановления данных после загрузки профиля.
    /// </summary>
    private Project? _pendingProjectData;

    /// <summary>
    /// Загружает существующий проект для редактирования.
    /// Восстанавливает все атрибуты, группы выбора и табличные данные.
    /// </summary>
    public void LoadExistingProject(Project project)
    {
        CurrentProject = project;
        IsEditMode = true;
        
        // Сохраняем проект для восстановления данных после загрузки профиля
        _pendingProjectData = project;
        
        // 1. Загрузить путь проекта (извлекаем родительскую папку из RootPath проекта)
        var projectFolderPath = project.RootPath;
        if (!string.IsNullOrEmpty(projectFolderPath))
        {
            var parentPath = Directory.GetParent(projectFolderPath)?.FullName;
            if (!string.IsNullOrEmpty(parentPath))
            {
                ProjectPath = parentPath;
            }
            else
            {
                ProjectPath = projectFolderPath;
            }
        }
        
        // 2. Загрузить профиль — это вызовет OnSelectedProfileChanged → LoadProfileSelections
        // LoadProfileSelections проверит _pendingProjectData и восстановит данные
        var profile = Profiles.FirstOrDefault(p => p.Id == project.ProfileId);
        if (profile != null)
        {
            SelectedProfile = profile;
        }
        
        StatusMessage = $"Проект '{project.Name}' загружен для редактирования.";
    }

    /// <summary>
    /// Восстанавливает данные проекта в UI-коллекции.
    /// Вызывается после загрузки профиля.
    /// </summary>
    private void RestoreProjectData(Project project)
    {
        // 1. Восстановить атрибуты из project.AttributeValues
        foreach (var attr in DynamicAttributes)
        {
            if (project.AttributeValues.TryGetValue(attr.Key, out var val))
            {
                attr.Value = val?.ToString() ?? string.Empty;
            }
        }
        
        // 2. Восстановить выбор из project.CompositionSelections
        foreach (var group in DynamicSelectionGroups)
        {
            if (project.CompositionSelections.TryGetValue(group.Key, out var selectedCodes))
            {
                foreach (var item in group.Items)
                {
                    item.IsSelected = selectedCodes.Contains(item.Code);
                }
            }
            else
            {
                // Если нет сохранённого выбора — снимаем все галочки (кроме дефолтных)
                foreach (var item in group.Items)
                {
                    item.IsSelected = false;
                }
            }
        }
        
        // 3. Восстановить табличные данные из project.TableData
        foreach (var table in DynamicTableFields)
        {
            if (project.TableData.TryGetValue(table.Key, out var rows))
            {
                table.Rows.Clear();
                foreach (var rowData in rows)
                {
                    var row = new TableRowItem
                    {
                        Code = rowData.GetValueOrDefault("Code")?.ToString() ?? "",
                        Name = rowData.GetValueOrDefault("Name")?.ToString() ?? ""
                    };
                    table.Rows.Add(row);
                }
            }
        }
        
        // 4. Восстановить структуру дерева из project.SavedStructure
        if (project.SavedStructure != null)
        {
            // Пересканировать файловую систему для определения актуального статуса папок
            RefreshNodeStatus(project.SavedStructure);
            PreviewStructure = new ObservableCollection<GeneratedNode> { project.SavedStructure };
            IsPreviewGenerated = true;
        }
    }

    /// <summary>
    /// Рекурсивно обновляет статус узлов на основе существования папок на диске.
    /// Также находит папки, существующие на диске, но отсутствующие в структуре (кандидаты на удаление).
    /// </summary>
    private void RefreshNodeStatus(GeneratedNode node)
    {
        bool exists = Directory.Exists(node.FullPath);
        
        // Если узел уже помечен как Delete (например, добавлен на предыдущем шаге рекурсии), не меняем статус
        if (node.Operation != NodeOperation.Delete)
        {
            node.Exists = exists;
            node.Operation = exists ? NodeOperation.None : NodeOperation.Create;
        }

        // Если папка существует, проверяем её содержимое на наличие удаляемых подпапок
        if (exists)
        {
            // Получаем список реальных подпапок на диске
            var subDirectories = Directory.GetDirectories(node.FullPath);
            var existingFolderNames = subDirectories.Select(Path.GetFileName).ToHashSet(StringComparer.OrdinalIgnoreCase);
            
            // Проходим по дочерним узлам из структуры
            foreach (var child in node.Children)
            {
                RefreshNodeStatus(child);
                // Убираем из списка найденных те, что есть в структуре
                existingFolderNames.Remove(child.Name); // Используем Name, предполагая что он совпадает с именем папки
            }

            // Оставшиеся папки — это те, которых нет в структуре, но есть на диске (Delete)
            foreach (var extraFolder in existingFolderNames)
            {
                if (extraFolder == null) continue;
                
                // Создаём узел-призрак для отображения удаляемой папки
                var deleteNode = new GeneratedNode
                {
                    Name = extraFolder,
                    FullPath = Path.Combine(node.FullPath, extraFolder),
                    Operation = NodeOperation.Delete,
                    IsIncluded = false, // Не участвует в генерации
                    Exists = true
                };

                // Рекурсивно помечаем всё содержимое как Delete
                MarkAllChildrenAsDelete(deleteNode);

                node.Children.Add(deleteNode);
            }
        }
        else
        {
            // Если папки нет, то и подпапок проверять нечего, но рекурсию для детей структуры продолжаем
            foreach (var child in node.Children)
            {
                RefreshNodeStatus(child);
            }
        }
    }

    private void MarkAllChildrenAsDelete(GeneratedNode node)
    {
        if (!Directory.Exists(node.FullPath)) return;

        foreach (var subDir in Directory.GetDirectories(node.FullPath))
        {
            var subDirName = Path.GetFileName(subDir);
            var childNode = new GeneratedNode
            {
                Name = subDirName,
                FullPath = subDir,
                Operation = NodeOperation.Delete,
                IsIncluded = false,
                Exists = true
            };
            MarkAllChildrenAsDelete(childNode);
            node.Children.Add(childNode);
        }
    }

    private void LoadProfileSelections(ProfileSchema profile)
    {
        DynamicSelectionGroups.Clear();
        DynamicTableFields.Clear();
        DynamicAttributes.Clear();
        
        // Загружаем только атрибуты проекта (IsProjectAttribute = true)
        foreach (var attrDef in profile.ProjectAttributes
            .Where(a => a.IsProjectAttribute)
            .OrderBy(a => a.Order))
        {
            var attrValue = new AttributeValueItem
            {
                Key = attrDef.Key,
                DisplayName = attrDef.DisplayName,
                AttributeType = attrDef.Type.ToString(),
                IsRequired = attrDef.IsRequired,
                Value = attrDef.DefaultValue ?? string.Empty,
                Description = attrDef.Description,
                DictionaryKey = attrDef.DictionaryKey
            };
            
            // Если атрибут типа Select/MultiSelect — загружаем элементы из словаря
            if (!string.IsNullOrEmpty(attrDef.DictionaryKey) && 
                (attrDef.Type == Domain.Enums.AttributeType.Select || attrDef.Type == Domain.Enums.AttributeType.MultiSelect))
            {
                var dict = profile.Dictionaries.FirstOrDefault(d => d.Key == attrDef.DictionaryKey);
                if (dict != null)
                {
                    foreach (var item in dict.Items)
                    {
                        attrValue.SelectItems.Add(new SelectableItem(item.Code, item.Name, false));
                    }
                }
            }
            
            DynamicAttributes.Add(attrValue);
        }
        
        // Собираем все SourceKey из структуры профиля (рекурсивно)
        var usedSourceKeys = new HashSet<string>();
        CollectAllSourceKeys(profile.Structure.RootNodes, usedSourceKeys);
        
        // Для каждого используемого SourceKey находим словарь и создаём элемент UI
        foreach (var sourceKey in usedSourceKeys)
        {
            var dict = profile.Dictionaries.FirstOrDefault(d => d.Key == sourceKey);
            if (dict == null) continue;
            
            if (dict.IsDynamic)
            {
                // Динамический словарь — показываем как TableField (ввод при создании проекта)
                var tableField = new TableField
                {
                    Key = dict.Key,
                    DisplayName = dict.DisplayName
                };
                DynamicTableFields.Add(tableField);
            }
            else
            {
                // Статический словарь — показываем как чекбоксы
                var group = new SelectionGroup
                {
                    Key = dict.Key,
                    DisplayName = dict.DisplayName
                };
                
                foreach (var item in dict.Items)
                {
                    // SystemFolders по умолчанию выбраны, остальные — нет
                    var defaultSelected = sourceKey == "SystemFolders";
                    group.Items.Add(new SelectableItem(item.Code, item.Name, defaultSelected));
                }
                
                DynamicSelectionGroups.Add(group);
            }
        }

        // Если есть ожидающий проект — восстанавливаем его данные
        if (_pendingProjectData != null)
        {
            RestoreProjectData(_pendingProjectData);
            _pendingProjectData = null;
            StatusMessage = $"Проект '{CurrentProject?.Name}' загружен для редактирования.";
        }
        else
        {
            StatusMessage = $"Профиль '{profile.Name}' загружен. Выберите нужные элементы.";
        }
    }

    /// <summary>
    /// Рекурсивно собирает все SourceKey из структуры профиля.
    /// </summary>
    private void CollectAllSourceKeys(IEnumerable<StructureNodeDefinition> nodes, HashSet<string> sourceKeys)
    {
        foreach (var node in nodes)
        {
            if (!string.IsNullOrEmpty(node.SourceKey))
            {
                sourceKeys.Add(node.SourceKey);
            }
            CollectAllSourceKeys(node.Children, sourceKeys);
        }
    }

    /// <summary>
    /// Рекурсивно собирает все SourceKey из структуры профиля,
    /// разделяя их на словарные (Dictionary) и табличные (Table).
    /// </summary>
    private void CollectSourceKeys(IEnumerable<StructureNodeDefinition> nodes, 
                                   HashSet<string> dictionarySources, 
                                   HashSet<string> tableSources)
    {
        foreach (var node in nodes)
        {
            if (!string.IsNullOrEmpty(node.SourceKey))
            {
                if (node.Multiplicity == Domain.Enums.MultiplicitySource.Dictionary)
                {
                    dictionarySources.Add(node.SourceKey);
                }
                else if (node.Multiplicity == Domain.Enums.MultiplicitySource.Table)
                {
                    tableSources.Add(node.SourceKey);
                }
            }
            CollectSourceKeys(node.Children, dictionarySources, tableSources);
        }
    }

    [RelayCommand]
    private async Task LoadProfiles()
    {
        var list = await _profileRepository.GetAllAsync();
        Profiles = new ObservableCollection<ProfileSchema>(list);
        if (Profiles.Any()) SelectedProfile = Profiles.First();
    }

    [RelayCommand]
    private async Task GeneratePreview()
    {
        if (SelectedProfile == null)
        {
            StatusMessage = "Выберите профиль!";
            return;
        }

        if (string.IsNullOrWhiteSpace(ProjectCode))
        {
            StatusMessage = "Введите шифр проекта!";
            return;
        }

        if (string.IsNullOrWhiteSpace(ProjectName))
        {
            StatusMessage = "Введите название проекта!";
            return;
        }

        // Проверяем что хотя бы в одной группе есть выбранные элементы
        var hasSelection = DynamicSelectionGroups.Any(g => g.SelectedItems.Any());
        if (!hasSelection)
        {
            StatusMessage = "Выберите хотя бы один элемент в списках!";
            return;
        }

        StatusMessage = "Генерация превью...";
        
        try
        {
            // Создаём Project с выбранными элементами
            var project = new Project
            {
                Name = ProjectName,
                RootPath = ProjectPath,
                ProfileId = SelectedProfile.Id
            };
            
            // Заполняем атрибуты проекта из динамических полей
            foreach (var attr in DynamicAttributes)
            {
                project.AttributeValues[attr.Key] = attr.Value;
            }
            
            // Сохраняем выбранные элементы из всех динамических групп
            foreach (var group in DynamicSelectionGroups)
            {
                project.CompositionSelections[group.Key] = group.SelectedCodes.ToList();
            }

            // Генерируем превью с учётом выбранных элементов
            var rootNode = GenerateStructureWithSelections(project, SelectedProfile);
            
            // Проверяем существование папок на диске для отображения статуса NEW
            RefreshNodeStatus(rootNode);
            
            PreviewStructure = new ObservableCollection<GeneratedNode> { rootNode };
            IsPreviewGenerated = true;
            
            // Подсчитываем статистику
            var totalFolders = CountNodes(rootNode);
            var newFolders = CountNodesByOperation(rootNode, NodeOperation.Create);
            var deletedFolders = CountNodesByOperation(rootNode, NodeOperation.Delete);
            var existingFolders = totalFolders - newFolders - deletedFolders;
            
            var msg = new List<string>();
            if (newFolders > 0) msg.Add($"Новых: {newFolders}");
            if (deletedFolders > 0) msg.Add($"Удаляются: {deletedFolders}");
            if (existingFolders > 0) msg.Add($"Без изменений: {existingFolders}");
            
            StatusMessage = $"Превью готово. {string.Join(", ", msg)}.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка: {ex.Message}";
        }
        await Task.CompletedTask;
    }

    private GeneratedNode GenerateStructureWithSelections(Project project, ProfileSchema profile)
    {
        var rootPath = Path.Combine(project.RootPath, $"{ProjectCode}_{ProjectShortName}");
        
        var rootNode = new GeneratedNode
        {
            NodeTypeId = "ProjectRoot",
            Name = $"{ProjectCode}_{ProjectShortName}",
            FullPath = rootPath
        };
        
        // Добавляем атрибуты проекта в контекст корня
        // Это позволит использовать {ProjectCode}, {Client} и т.д. во всех дочерних узлах
        foreach (var attr in DynamicAttributes)
        {
            rootNode.ContextAttributes[attr.Key] = attr.Value;
        }

        // Рекурсивно генерируем структуру на основе определения профиля
        foreach (var structureDef in profile.Structure.RootNodes)
        {
            GenerateNodesRecursive(structureDef, rootNode, profile);
        }

        return rootNode;
    }

    /// <summary>
    /// Рекурсивно генерирует узлы на основе определения структуры.
    /// Использует SourceKey для поиска словаря. Если словарь IsDynamic — берёт из TableField,
    /// иначе — из SelectionGroup. Контекст родителя наследуется дочерними узлами.
    /// </summary>
    private void GenerateNodesRecursive(StructureNodeDefinition definition, GeneratedNode parent, ProfileSchema profile)
    {
        var nodeType = profile.NodeTypes.FirstOrDefault(nt => nt.TypeId == definition.NodeTypeId);
        var formula = definition.NamingFormulaOverride ?? nodeType?.DefaultFormula ?? "{Code}";

        if (definition.Multiplicity == Domain.Enums.MultiplicitySource.Single || string.IsNullOrEmpty(definition.SourceKey))
        {
            // Single — один узел
            var node = new GeneratedNode
            {
                NodeTypeId = definition.NodeTypeId,
                Name = definition.NodeTypeId,
                FullPath = Path.Combine(parent.FullPath, definition.NodeTypeId),
                NameFormula = formula
            };
            
            // Наследуем контекст от родителя
            foreach (var ctx in parent.ContextAttributes)
            {
                node.ContextAttributes[ctx.Key] = ctx.Value;
            }

            parent.Children.Add(node);

            foreach (var childDef in definition.Children)
            {
                GenerateNodesRecursive(childDef, node, profile);
            }
            return;
        }

        // Ищем словарь по SourceKey
        var dict = profile.Dictionaries.FirstOrDefault(d => d.Key == definition.SourceKey);
        if (dict == null) return;
        
        // Собираем элементы: из TableField (если IsDynamic) или из SelectionGroup
        List<(string Code, string Name)> items;
        
        if (dict.IsDynamic)
        {
            // Динамический словарь — берём из TableField
            var tableField = DynamicTableFields.FirstOrDefault(t => t.Key == definition.SourceKey);
            items = tableField?.Rows.Select(r => (r.Code, r.Name)).ToList() ?? new();
        }
        else
        {
            // Статический словарь — берём выбранные элементы из SelectionGroup
            var group = DynamicSelectionGroups.FirstOrDefault(g => g.Key == definition.SourceKey);
            items = group?.SelectedItems.Select(i => (i.Code, i.Name)).ToList() ?? new();
        }

        foreach (var (code, name) in items)
        {
            var nodeName = $"{code}_{name}";
            var node = new GeneratedNode
            {
                NodeTypeId = definition.NodeTypeId,
                Name = nodeName,
                FullPath = Path.Combine(parent.FullPath, nodeName),
                NameFormula = formula
            };
            
            // Наследуем контекст от родителя
            foreach (var ctx in parent.ContextAttributes)
            {
                node.ContextAttributes[ctx.Key] = ctx.Value;
            }
            
            // Добавляем контекст текущего узла (используем SourceKey как ключ)
            node.ContextAttributes[definition.SourceKey] = new Dictionary<string, object>
            {
                ["Code"] = code,
                ["Name"] = name
            };

            parent.Children.Add(node);

            // Рекурсивно генерируем дочерние узлы
            foreach (var childDef in definition.Children)
            {
                GenerateNodesRecursive(childDef, node, profile);
            }
        }
    }


    private int CountNodes(GeneratedNode node)
    {
        return 1 + node.Children.Sum(c => CountNodes(c));
    }

    private int CountNodesByOperation(GeneratedNode node, NodeOperation operation)
    {
        var count = node.Operation == operation ? 1 : 0;
        return count + node.Children.Sum(c => CountNodesByOperation(c, operation));
    }

    [RelayCommand]
    private async Task CreateProject()
    {
        if (!IsPreviewGenerated || PreviewStructure.Count == 0 || SelectedProfile == null) return;

        StatusMessage = IsEditMode ? "Обновление проекта..." : "Создание проекта...";
        try
        {
            var rootNode = PreviewStructure[0];
            
            // Используем существующий проект в режиме редактирования или создаём новый
            var project = IsEditMode && CurrentProject != null 
                ? CurrentProject 
                : new Project();
            
            // Обновляем базовые свойства проекта
            project.Name = ProjectName;
            project.RootPath = rootNode.FullPath; // Корневой путь теперь включает имя папки проекта
            project.ProfileId = SelectedProfile!.Id;
            
            // Сохраняем атрибуты проекта
            project.AttributeValues.Clear();
            foreach (var attr in DynamicAttributes)
            {
                project.AttributeValues[attr.Key] = attr.Value;
            }
            
            // Сохраняем выбранные элементы из всех групп
            project.CompositionSelections.Clear();
            foreach (var group in DynamicSelectionGroups)
            {
                project.CompositionSelections[group.Key] = group.SelectedCodes.ToList();
            }
            
            // Сохраняем табличные данные
            project.TableData.Clear();
            foreach (var table in DynamicTableFields)
            {
                var rows = table.Rows.Select(r => new Dictionary<string, object>
                {
                    ["Code"] = r.Code,
                    ["Name"] = r.Name
                }).ToList();
                project.TableData[table.Key] = rows;
            }
            
            // Сохраняем структуру дерева для восстановления при редактировании
            project.SavedStructure = rootNode;
            
            await _projectManagerService.CreateProjectAsync(project, SelectedProfile, rootNode);

            StatusMessage = IsEditMode 
                ? "Проект успешно обновлён!" 
                : "Проект успешно создан!";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка: {ex.Message}";
        }
    }

    [RelayCommand]
    private void SelectPath()
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog();
        dialog.Title = "Выберите корневую папку";
        if (Directory.Exists(ProjectPath))
        {
            dialog.InitialDirectory = ProjectPath;
        }

        if (dialog.ShowDialog() == true)
        {
            ProjectPath = dialog.FolderName;
        }
    }

    /// <summary>
    /// Универсальная команда для выбора всех элементов группы.
    /// </summary>
    [RelayCommand]
    private void SelectAllInGroup(SelectionGroup? group) => group?.SelectAll();

    /// <summary>
    /// Универсальная команда для снятия выбора со всех элементов группы.
    /// </summary>
    [RelayCommand]
    private void DeselectAllInGroup(SelectionGroup? group) => group?.DeselectAll();

    /// <summary>
    /// Универсальная команда для добавления строки в табличное поле.
    /// </summary>
    [RelayCommand]
    private void AddTableRow(TableField? tableField) => tableField?.AddRow();

    /// <summary>
    /// Универсальная команда для удаления строки из табличного поля.
    /// </summary>
    [RelayCommand]
    private void RemoveTableRow(TableField? tableField)
    {
        if (tableField != null && SelectedTableRow != null)
        {
            tableField.RemoveRow(SelectedTableRow);
        }
    }

    [RelayCommand]
    private void CopyPath(GeneratedNode? node)
    {
        if (node != null && !string.IsNullOrWhiteSpace(node.FullPath))
        {
            try
            {
                Clipboard.SetText(node.FullPath);
                StatusMessage = "Путь скопирован в буфер обмена";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Не удалось скопировать путь: {ex.Message}";
            }
        }
    }

    [RelayCommand]
    private void OpenNodeInExplorer(GeneratedNode? node)
    {
        if (node != null && !string.IsNullOrWhiteSpace(node.FullPath))
        {
            if (Directory.Exists(node.FullPath))
            {
                Process.Start("explorer.exe", node.FullPath);
            }
            else
            {
                StatusMessage = $"Папка еще не создана: {node.FullPath}";
            }
        }
    }

    [RelayCommand]
    private void RecalculateNames()
    {
        if (!IsPreviewGenerated || PreviewStructure.Count == 0 || SelectedProfile == null)
        {
            StatusMessage = "Сначала сгенерируйте превью!";
            return;
        }

        try
        {
            StatusMessage = "Пересчёт имён...";
            
            var context = new Dictionary<string, string>
            {
                ["Project.Code"] = ProjectCode,
                ["Project.Name"] = ProjectName,
                ["Project.ShortName"] = ProjectShortName,
                ["Project.RootPath"] = ProjectPath
            };

            RecalculateNodeNames(PreviewStructure[0], context, ProjectPath);
            
            var temp = PreviewStructure;
            PreviewStructure = new ObservableCollection<GeneratedNode>();
            PreviewStructure = temp;
            
            StatusMessage = "Имена пересчитаны.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка пересчёта: {ex.Message}";
        }
    }

    private void RecalculateNodeNames(GeneratedNode node, Dictionary<string, string> context, string parentPath)
    {
        var nodeContext = new Dictionary<string, string>(context);
        if (!string.IsNullOrEmpty(node.NodeTypeId))
        {
            nodeContext[$"{node.NodeTypeId}.Name"] = node.Name;
        }

        if (!string.IsNullOrEmpty(node.NameFormula))
        {
            node.Name = _namingEngine.ApplyFormula(node.NameFormula, nodeContext);
        }

        node.FullPath = Path.Combine(parentPath, node.Name);

        foreach (var child in node.Children)
        {
            RecalculateNodeNames(child, nodeContext, node.FullPath);
        }
    }

    [RelayCommand]
    private void ViewAcl(GeneratedNode? node)
    {
        if (node == null || string.IsNullOrWhiteSpace(node.FullPath))
        {
            StatusMessage = "Выберите узел для просмотра ACL";
            return;
        }

        if (!Directory.Exists(node.FullPath))
        {
            StatusMessage = $"Папка еще не создана: {node.FullPath}";
            return;
        }

        var viewModel = new AclViewerViewModel(_aclService);
        viewModel.LoadAcl(node.FullPath);

        var dialog = new AclViewerDialog
        {
            DataContext = viewModel,
            Owner = System.Windows.Application.Current.MainWindow
        };
        dialog.ShowDialog();
    }
}
