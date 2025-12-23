using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using Microsoft.Extensions.DependencyInjection;
using AGK.ProjectGen.Application.Interfaces;
using AGK.ProjectGen.Domain.AccessControl;
using AGK.ProjectGen.Domain.Enums;

namespace AGK.ProjectGen.UI.ViewModels;

public partial class AclRuleEditorViewModel : ObservableObject
{
    private readonly ISecurityPrincipalRepository _principalRepository;
    private ObservableCollection<AclRuleDefinition> _originalRules = new();
    
    [ObservableProperty]
    private ObservableCollection<AclRuleDefinition> _rules = new();
    
    [ObservableProperty]
    private AclRuleDefinition? _selectedRule;
    
    // Для отслеживания предыдущего правила и сохранения его изменений при переключении
    private AclRuleDefinition? _previousSelectedRule;
    
    [ObservableProperty]
    private ObservableCollection<SecurityPrincipal> _availablePrincipals = new();
    
    [ObservableProperty]
    private ObservableCollection<AclConditionViewModel> _currentConditions = new();
    
    [ObservableProperty]
    private AclConditionViewModel? _selectedCondition;
    
    [ObservableProperty]
    private SecurityPrincipal? _selectedPrincipal;
    
    [ObservableProperty]
    private string _customPrincipalName = string.Empty;
    
    [ObservableProperty]
    private string _nodeTypeInfo = string.Empty;
    
    [ObservableProperty]
    private string _presetName = string.Empty;
    
    [ObservableProperty]
    private string _principalSearchQuery = string.Empty;
    
    [ObservableProperty]
    private ObservableCollection<SecurityPrincipal> _filteredPrincipals = new();
    
    [ObservableProperty]
    private bool _isSearchDropdownOpen;
    
    public bool HasRules => Rules.Count > 0;
    
    public string[] AvailableOperators { get; } = new[]
    {
        "== (Равно)",
        "!= (Не равно)",
        "Contains (Содержит)",
        "NotContains (Не содержит)",
        "> (Больше)",
        "< (Меньше)",
        ">= (Больше или равно)",
        "<= (Меньше или равно)"
    };
    
    public string[] AvailableRights { get; } = new[]
    {
        "Read",
        "Write",
        "Modify",
        "FullControl"
    };
    
    [ObservableProperty]
    private ObservableCollection<string> _commonAttributes = new();
    
    /// <summary>
    /// Загруженные словари с элементами для автодополнения значений.
    /// </summary>
    private IEnumerable<Domain.Schema.DictionarySchema>? _loadedDictionaries;
    
    public AclRuleEditorViewModel(ISecurityPrincipalRepository principalRepository)
    {
        _principalRepository = principalRepository;
        _ = LoadPrincipalsAsync();
    }
    
    public void LoadRules(ObservableCollection<AclRuleDefinition> rules, string nodeTypeInfo)
    {
        NodeTypeInfo = nodeTypeInfo;
        _originalRules = rules;
        Rules = new ObservableCollection<AclRuleDefinition>(rules);
        OnPropertyChanged(nameof(HasRules));
    }
    
    /// <summary>
    /// Загружает список доступных атрибутов на основе словарей и атрибутов профиля.
    /// </summary>
    public void LoadDictionaries(IEnumerable<Domain.Schema.DictionarySchema> dictionaries)
    {
        LoadAttributes(dictionaries, null);
    }
    
    /// <summary>
    /// Загружает список доступных атрибутов на основе словарей и атрибутов профиля.
    /// </summary>
    public void LoadAttributes(
        IEnumerable<Domain.Schema.DictionarySchema>? dictionaries, 
        IEnumerable<Domain.Schema.FieldSchema>? attributes)
    {
        // Сохраняем словари для автодополнения значений
        _loadedDictionaries = dictionaries?.ToList();
        
        CommonAttributes.Clear();
        
        // Добавляем базовые атрибуты проекта
        CommonAttributes.Add("Project.Name");
        CommonAttributes.Add("Project.Code");
        
        // Добавляем пользовательские атрибуты профиля
        if (attributes != null)
        {
            foreach (var attr in attributes)
            {
                CommonAttributes.Add($"Project.{attr.Key}");
            }
        }
        
        // Добавляем атрибуты из каждого словаря
        if (dictionaries != null)
        {
            foreach (var dict in dictionaries)
            {
                CommonAttributes.Add($"{dict.Key}.Code");
                CommonAttributes.Add($"{dict.Key}.Name");
            }
        }
    }
    
    public ObservableCollection<AclRuleDefinition> GetRules() => Rules;
    
    /// <summary>
    /// Применяет изменения к оригинальной коллекции правил.
    /// </summary>
    public void ApplyChanges()
    {
        // Сначала применяем изменения условий для текущего правила
        ApplyConditionChanges();
        
        _originalRules.Clear();
        foreach (var rule in Rules)
        {
            _originalRules.Add(rule);
        }
    }
    
    private async Task LoadPrincipalsAsync()
    {
        var principals = await _principalRepository.GetAllPrincipalsAsync();
        AvailablePrincipals = new ObservableCollection<SecurityPrincipal>(principals);
        
        // После обновления коллекции восстанавливаем выбор
        UpdateSelectedPrincipal();
    }
    
    partial void OnSelectedRuleChanged(AclRuleDefinition? value)
    {
        // Сначала сохраняем изменения условий для предыдущего правила
        if (_previousSelectedRule != null && CurrentConditions.Count > 0)
        {
            ApplyConditionChangesToRule(_previousSelectedRule);
        }
        
        CurrentConditions.Clear();
        if (value != null)
        {
            foreach (var condition in value.Conditions)
            {
                CurrentConditions.Add(new AclConditionViewModel(condition, _loadedDictionaries));
            }
            
            // Синхронизируем SelectedPrincipal с PrincipalIdentity текущего правила
            UpdateSelectedPrincipal();
        }
        else
        {
            SelectedPrincipal = null;
        }
        
        // Запоминаем текущее правило как предыдущее для следующего переключения
        _previousSelectedRule = value;
    }
    
    partial void OnSelectedPrincipalChanged(SecurityPrincipal? value)
    {
        // При изменении выбора в ComboBox обновляем PrincipalIdentity правила
        if (SelectedRule != null && value != null)
        {
            SelectedRule.PrincipalIdentity = value.FullName;
            // Закрываем dropdown и обновляем поисковое поле
            PrincipalSearchQuery = value.FullName;
            IsSearchDropdownOpen = false;
        }
    }
    
    partial void OnPrincipalSearchQueryChanged(string value)
    {
        FilterPrincipals(value);
    }
    
    /// <summary>
    /// Фильтрует список субъектов безопасности по запросу.
    /// Умный поиск: по частям имени, регистронезависимый.
    /// </summary>
    private void FilterPrincipals(string query)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
        {
            FilteredPrincipals.Clear();
            IsSearchDropdownOpen = false;
            return;
        }
        
        // Разбиваем запрос на части
        var queryParts = query
            .Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(p => p.Length >= 1)
            .Select(p => p.ToLowerInvariant())
            .ToArray();
        
        if (queryParts.Length == 0)
        {
            FilteredPrincipals.Clear();
            IsSearchDropdownOpen = false;
            return;
        }
        
        // Фильтруем по всем частям запроса
        var filtered = AvailablePrincipals
            .Where(p =>
            {
                var searchText = $"{p.Name} {p.Domain} {p.Description}".ToLowerInvariant();
                return queryParts.All(part => searchText.Contains(part));
            })
            .OrderByDescending(p => queryParts.Any(part => 
                p.Name.StartsWith(part, StringComparison.OrdinalIgnoreCase)))
            .ThenBy(p => p.Name)
            .Take(20)
            .ToList();
        
        FilteredPrincipals = new ObservableCollection<SecurityPrincipal>(filtered);
        IsSearchDropdownOpen = filtered.Count > 0;
    }
    
    /// <summary>
    /// Выбирает субъекта безопасности из результатов поиска.
    /// </summary>
    [RelayCommand]
    private void SelectPrincipalFromSearch(SecurityPrincipal? principal)
    {
        if (principal != null && SelectedRule != null)
        {
            SelectedRule.PrincipalIdentity = principal.FullName;
            SelectedPrincipal = principal;
            PrincipalSearchQuery = principal.FullName;
            IsSearchDropdownOpen = false;
            OnPropertyChanged(nameof(SelectedRule));
        }
    }
    
    /// <summary>
    /// Находит и устанавливает SelectedPrincipal на основе PrincipalIdentity текущего правила.
    /// </summary>
    private void UpdateSelectedPrincipal()
    {
        if (SelectedRule == null || string.IsNullOrEmpty(SelectedRule.PrincipalIdentity))
        {
            SelectedPrincipal = null;
            PrincipalSearchQuery = string.Empty;
            return;
        }
        
        // Ищем principal по FullName
        SelectedPrincipal = AvailablePrincipals.FirstOrDefault(p => 
            p.FullName.Equals(SelectedRule.PrincipalIdentity, StringComparison.OrdinalIgnoreCase));
        
        // Обновляем поисковое поле
        PrincipalSearchQuery = SelectedRule.PrincipalIdentity;
    }
    
    [RelayCommand]
    private void AddRule()
    {
        var newRule = new AclRuleDefinition
        {
            Description = "Новое правило",
            Rights = ProjectAccessRights.Read
        };
        Rules.Add(newRule);
        SelectedRule = newRule;
        OnPropertyChanged(nameof(HasRules));
    }
    
    [RelayCommand]
    private void RemoveRule()
    {
        if (SelectedRule != null)
        {
            Rules.Remove(SelectedRule);
            SelectedRule = Rules.FirstOrDefault();
            OnPropertyChanged(nameof(HasRules));
        }
    }
    
    [RelayCommand]
    private void AddCondition()
    {
        if (SelectedRule == null) return;
        
        // Добавляем только в UI-коллекцию (копия)
        // Изменения применятся к SelectedRule.Conditions при ApplyConditionChanges()
        var newCondition = new AclConditionViewModel(_loadedDictionaries);
        CurrentConditions.Add(newCondition);
    }
    
    /// <summary>
    /// Удаляет все условия, у которых установлен чекбокс IsSelected.
    /// </summary>
    [RelayCommand]
    private void RemoveCondition()
    {
        if (SelectedRule == null) return;
        
        // Собираем выбранные условия
        var selectedItems = CurrentConditions.Where(c => c.IsSelected).ToList();
        
        if (selectedItems.Count == 0)
        {
            System.Windows.MessageBox.Show(
                "Выберите условия для удаления (отметьте чекбоксами)",
                "Нет выбранных условий",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
            return;
        }
        
        // Удаляем из UI-коллекции (изменения применятся при сохранении)
        foreach (var item in selectedItems)
        {
            CurrentConditions.Remove(item);
        }
    }
    
    /// <summary>
    /// Применяет изменения условий к оригинальному правилу.
    /// Вызывается перед сохранением диалога.
    /// </summary>
    public void ApplyConditionChanges()
    {
        // Применяем изменения для текущего выбранного правила
        if (SelectedRule != null)
        {
            ApplyConditionChangesToRule(SelectedRule);
        }
    }
    
    /// <summary>
    /// Применяет изменения условий из CurrentConditions к указанному правилу.
    /// </summary>
    private void ApplyConditionChangesToRule(AclRuleDefinition rule)
    {
        if (rule == null) return;
        
        // Перестраиваем коллекцию условий из UI-копий
        rule.Conditions.Clear();
        foreach (var conditionVm in CurrentConditions)
        {
            rule.Conditions.Add(conditionVm.ApplyToOriginal());
        }
    }
    
    [RelayCommand]
    private void ApplyCustomPrincipal()
    {
        if (SelectedRule != null && !string.IsNullOrWhiteSpace(CustomPrincipalName))
        {
            SelectedRule.PrincipalIdentity = CustomPrincipalName;
            OnPropertyChanged(nameof(SelectedRule));
        }
    }
    
    #region Preset Commands
    
    /// <summary>
    /// Сохранить текущие правила как новый пресет.
    /// </summary>
    [RelayCommand]
    private async Task SaveAsPreset()
    {
        if (Rules.Count == 0)
        {
            System.Windows.MessageBox.Show(
                "Добавьте хотя бы одно правило перед сохранением пресета",
                "Нет правил",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
            return;
        }
        
        // Показываем диалог ввода имени
        var dialog = new Views.InputDialog("Сохранение пресета", "Введите название пресета:", "Новый пресет");
        if (dialog.ShowDialog() != true || string.IsNullOrWhiteSpace(dialog.InputValue))
        {
            return;
        }
        
        var presetRepository = App.Current.Services.GetService<IAclPresetRepository>();
        if (presetRepository == null)
        {
            System.Windows.MessageBox.Show("Сервис пресетов недоступен", "Ошибка",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            return;
        }
        
        // Создаём пресет с копиями правил
        var preset = new AclPreset
        {
            Name = dialog.InputValue,
            Description = $"Создан из: {NodeTypeInfo}",
            Rules = new ObservableCollection<AclRuleDefinition>(
                Rules.Select(r => new AclRuleDefinition
                {
                    Id = Guid.NewGuid().ToString(),
                    PrincipalIdentity = r.PrincipalIdentity,
                    Rights = r.Rights,
                    IsDeny = r.IsDeny,
                    Description = r.Description,
                    Conditions = new List<AclCondition>(
                        r.Conditions.Select(c => new AclCondition
                        {
                            AttributePath = c.AttributePath,
                            Operator = c.Operator,
                            Value = c.Value
                        })
                    )
                })
            )
        };
        
        await presetRepository.SaveAsync(preset);
        
        System.Windows.MessageBox.Show(
            $"Пресет \"{preset.Name}\" сохранён",
            "Успешно",
            System.Windows.MessageBoxButton.OK,
            System.Windows.MessageBoxImage.Information);
    }
    
    /// <summary>
    /// Загрузить пресет и применить к текущим правилам.
    /// </summary>
    [RelayCommand]
    private async Task LoadPreset()
    {
        var presetRepository = App.Current.Services.GetService<IAclPresetRepository>();
        if (presetRepository == null)
        {
            System.Windows.MessageBox.Show("Сервис пресетов недоступен", "Ошибка",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            return;
        }
        
        var viewModel = new AclPresetDialogViewModel(presetRepository, _principalRepository);
        await viewModel.InitializeForLoad();
        
        var dialog = new Views.AclPresetDialog
        {
            DataContext = viewModel,
            Owner = System.Windows.Application.Current.MainWindow
        };
        
        if (dialog.ShowDialog() == true && dialog.PresetLoaded)
        {
            var selectedPreset = viewModel.GetSelectedPreset();
            if (selectedPreset != null)
            {
                // Спрашиваем: заменить или добавить?
                var result = System.Windows.MessageBox.Show(
                    $"Загрузить пресет \"{selectedPreset.Name}\"?\n\n" +
                    "Да — заменить текущие правила\n" +
                    "Нет — добавить к существующим",
                    "Способ загрузки",
                    System.Windows.MessageBoxButton.YesNoCancel,
                    System.Windows.MessageBoxImage.Question);
                
                if (result == System.Windows.MessageBoxResult.Cancel)
                    return;
                
                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    // Заменить
                    Rules.Clear();
                }
                
                // Добавляем правила из пресета (копии)
                foreach (var rule in selectedPreset.GetRulesCopy())
                {
                    Rules.Add(rule);
                }
                
                SelectedRule = Rules.FirstOrDefault();
                OnPropertyChanged(nameof(HasRules));
            }
        }
    }
    
    /// <summary>
    /// Открыть менеджер пресетов для просмотра и редактирования.
    /// </summary>
    [RelayCommand]
    private async Task ManagePresets()
    {
        var presetRepository = App.Current.Services.GetService<IAclPresetRepository>();
        if (presetRepository == null)
        {
            System.Windows.MessageBox.Show("Сервис пресетов недоступен", "Ошибка",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            return;
        }
        
        var viewModel = new AclPresetDialogViewModel(presetRepository, _principalRepository);
        await viewModel.InitializeForManage();
        
        var dialog = new Views.AclPresetDialog
        {
            DataContext = viewModel,
            Owner = System.Windows.Application.Current.MainWindow
        };
        
        dialog.ShowDialog();
    }
    
    /// <summary>
    /// Открыть менеджер групп и пользователей.
    /// </summary>
    [RelayCommand]
    private async Task ManageGroups()
    {
        var viewModel = new SecurityPrincipalsViewModel(_principalRepository);
        await viewModel.InitializeAsync();
        
        var dialog = new Views.SecurityPrincipalsDialog
        {
            DataContext = viewModel,
            Owner = System.Windows.Application.Current.MainWindow
        };
        
        dialog.ShowDialog();
        
        // Перезагружаем список доступных принципалов
        await LoadPrincipalsAsync();
    }
    
    #endregion
}

public partial class AclConditionViewModel : ObservableObject
{
    /// <summary>
    /// Оригинальное условие (для применения изменений при сохранении).
    /// </summary>
    public AclCondition? OriginalCondition { get; private set; }
    
    /// <summary>
    /// Признак нового условия (ещё не сохранённого).
    /// </summary>
    public bool IsNew { get; set; }
    
    /// <summary>
    /// Загруженные словари для получения реальных значений.
    /// </summary>
    public IEnumerable<Domain.Schema.DictionarySchema>? Dictionaries { get; set; }
    
    [ObservableProperty]
    private bool _isSelected;
    
    [ObservableProperty]
    private string _attributePath = string.Empty;
    
    [ObservableProperty]
    private ConditionOperator _operator = ConditionOperator.Equals;
    
    [ObservableProperty]
    private string _value = string.Empty;
    
    [ObservableProperty]
    private bool _isValueDropdownOpen;
    
    [ObservableProperty]
    private ObservableCollection<string> _filteredValueSuggestions = new();
    
    /// <summary>
    /// Создаёт ViewModel из существующего условия (копирует данные).
    /// </summary>
    public AclConditionViewModel(AclCondition condition, IEnumerable<Domain.Schema.DictionarySchema>? dictionaries = null)
    {
        OriginalCondition = condition;
        IsNew = false;
        Dictionaries = dictionaries;
        
        // Копируем данные
        _attributePath = condition.AttributePath;
        _operator = condition.Operator;
        _value = condition.Value;
    }
    
    /// <summary>
    /// Создаёт новую ViewModel для нового условия.
    /// </summary>
    public AclConditionViewModel(IEnumerable<Domain.Schema.DictionarySchema>? dictionaries = null)
    {
        OriginalCondition = null;
        IsNew = true;
        Dictionaries = dictionaries;
        _attributePath = "";
        _operator = ConditionOperator.Equals;
        _value = "";
    }
    
    partial void OnValueChanged(string value)
    {
        FilterValueSuggestions(value);
    }
    
    /// <summary>
    /// Фильтрует подсказки значений на основе выбранного AttributePath.
    /// При вводе "=" показывает реальные значения из соответствующего словаря.
    /// </summary>
    private void FilterValueSuggestions(string value)
    {
        // Показываем подсказки только если значение начинается с "="
        if (string.IsNullOrEmpty(value) || !value.StartsWith("="))
        {
            FilteredValueSuggestions.Clear();
            IsValueDropdownOpen = false;
            return;
        }
        
        // Получаем текст после "=" для фильтрации
        var searchText = value.Length > 1 ? value.Substring(1).ToLowerInvariant() : "";
        
        // Получаем значения на основе AttributePath
        var suggestions = GetValuesForAttributePath(searchText);
        
        FilteredValueSuggestions = new ObservableCollection<string>(suggestions);
        IsValueDropdownOpen = suggestions.Count > 0;
    }
    
    /// <summary>
    /// Получает реальные значения на основе выбранного AttributePath.
    /// Например, если AttributePath = "Stages.Code", возвращает коды элементов словаря Stages.
    /// </summary>
    private List<string> GetValuesForAttributePath(string searchText)
    {
        var result = new List<string>();
        
        if (string.IsNullOrEmpty(AttributePath))
            return result;
        
        // Разбираем AttributePath на части: "DictKey.Field"
        var parts = AttributePath.Split('.');
        if (parts.Length < 2)
            return result;
        
        var dictKey = parts[0];  // Например: "Stages", "Buildings", "Project"
        var fieldName = parts[1]; // Например: "Code", "Name"
        
        // Для атрибутов проекта (Project.Name, Project.Code) пока не можем получить значения
        // так как они определяются только при создании проекта
        if (dictKey.Equals("Project", StringComparison.OrdinalIgnoreCase))
        {
            return result;
        }
        
        // Ищем словарь по ключу
        if (Dictionaries == null)
            return result;
        
        var dictionary = Dictionaries.FirstOrDefault(d => 
            d.Key.Equals(dictKey, StringComparison.OrdinalIgnoreCase));
        
        if (dictionary == null || dictionary.Items == null || dictionary.Items.Count == 0)
            return result;
        
        // Получаем значения в зависимости от поля
        IEnumerable<string> values;
        if (fieldName.Equals("Code", StringComparison.OrdinalIgnoreCase))
        {
            values = dictionary.Items.Select(i => i.Code);
        }
        else if (fieldName.Equals("Name", StringComparison.OrdinalIgnoreCase))
        {
            values = dictionary.Items.Select(i => i.Name);
        }
        else
        {
            return result;
        }
        
        // Фильтруем по введённому тексту
        result = values
            .Where(v => !string.IsNullOrEmpty(v))
            .Where(v => string.IsNullOrEmpty(searchText) || 
                        v.ToLowerInvariant().Contains(searchText))
            .Distinct()
            .Take(15)
            .ToList();
        
        return result;
    }
    
    /// <summary>
    /// Выбирает подсказку из списка.
    /// </summary>
    [RelayCommand]
    private void SelectValueSuggestion(string suggestion)
    {
        Value = suggestion;
        IsValueDropdownOpen = false;
    }
    
    /// <summary>
    /// Применяет изменения к оригинальному условию или создаёт новое.
    /// </summary>
    public AclCondition ApplyToOriginal()
    {
        if (OriginalCondition != null)
        {
            OriginalCondition.AttributePath = AttributePath;
            OriginalCondition.Operator = Operator;
            OriginalCondition.Value = Value;
            return OriginalCondition;
        }
        else
        {
            return new AclCondition
            {
                AttributePath = AttributePath,
                Operator = Operator,
                Value = Value
            };
        }
    }
    
    /// <summary>
    /// Для обратной совместимости — возвращает текущее условие.
    /// </summary>
    public AclCondition Condition => OriginalCondition ?? new AclCondition 
    { 
        AttributePath = AttributePath, 
        Operator = Operator, 
        Value = Value 
    };
    
    public string DisplayText => $"{AttributePath} {Operator} '{Value}'";
}
