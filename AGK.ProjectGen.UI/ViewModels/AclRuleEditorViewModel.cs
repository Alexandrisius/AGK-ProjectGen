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
    
    [ObservableProperty]
    private ObservableCollection<SecurityPrincipal> _availablePrincipals = new();
    
    [ObservableProperty]
    private ObservableCollection<AclConditionViewModel> _currentConditions = new();
    
    [ObservableProperty]
    private AclConditionViewModel? _selectedCondition;
    
    [ObservableProperty]
    private string _customPrincipalName = string.Empty;
    
    [ObservableProperty]
    private string _nodeTypeInfo = string.Empty;
    
    [ObservableProperty]
    private string _presetName = string.Empty;
    
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
    }
    
    partial void OnSelectedRuleChanged(AclRuleDefinition? value)
    {
        CurrentConditions.Clear();
        if (value != null)
        {
            foreach (var condition in value.Conditions)
            {
                CurrentConditions.Add(new AclConditionViewModel(condition));
            }
        }
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
        var newCondition = new AclConditionViewModel();
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
        if (SelectedRule == null) return;
        
        // Перестраиваем коллекцию условий из UI-копий
        SelectedRule.Conditions.Clear();
        foreach (var conditionVm in CurrentConditions)
        {
            SelectedRule.Conditions.Add(conditionVm.ApplyToOriginal());
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
    
    [ObservableProperty]
    private bool _isSelected;
    
    [ObservableProperty]
    private string _attributePath = string.Empty;
    
    [ObservableProperty]
    private ConditionOperator _operator = ConditionOperator.Equals;
    
    [ObservableProperty]
    private string _value = string.Empty;
    
    /// <summary>
    /// Создаёт ViewModel из существующего условия (копирует данные).
    /// </summary>
    public AclConditionViewModel(AclCondition condition)
    {
        OriginalCondition = condition;
        IsNew = false;
        
        // Копируем данные
        _attributePath = condition.AttributePath;
        _operator = condition.Operator;
        _value = condition.Value;
    }
    
    /// <summary>
    /// Создаёт новую ViewModel для нового условия.
    /// </summary>
    public AclConditionViewModel()
    {
        OriginalCondition = null;
        IsNew = true;
        _attributePath = "Discipline.Code";
        _operator = ConditionOperator.Equals;
        _value = "";
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
