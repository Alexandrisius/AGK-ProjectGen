using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using AGK.ProjectGen.Application.Interfaces;
using AGK.ProjectGen.Domain.AccessControl;
using AGK.ProjectGen.Domain.Enums;

namespace AGK.ProjectGen.UI.ViewModels;

public partial class AclRuleEditorViewModel : ObservableObject
{
    private readonly ISecurityPrincipalRepository _principalRepository;
    
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
    
    public string[] CommonAttributes { get; } = new[]
    {
        "Discipline.Code",
        "Discipline.Name",
        "Stage.Code",
        "Stage.Name",
        "Queue.Code",
        "Queue.Name",
        "SystemFolder.Code",
        "SystemFolder.Name",
        "Building.Code",
        "Building.Name"
    };
    
    public AclRuleEditorViewModel(ISecurityPrincipalRepository principalRepository)
    {
        _principalRepository = principalRepository;
        _ = LoadPrincipalsAsync();
    }
    
    public void LoadRules(ObservableCollection<AclRuleDefinition> rules, string nodeTypeInfo)
    {
        NodeTypeInfo = nodeTypeInfo;
        Rules = new ObservableCollection<AclRuleDefinition>(rules);
    }
    
    public ObservableCollection<AclRuleDefinition> GetRules() => Rules;
    
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
    }
    
    [RelayCommand]
    private void RemoveRule()
    {
        if (SelectedRule != null)
        {
            Rules.Remove(SelectedRule);
            SelectedRule = Rules.FirstOrDefault();
        }
    }
    
    [RelayCommand]
    private void AddCondition()
    {
        if (SelectedRule == null) return;
        
        var condition = new AclCondition
        {
            AttributePath = "Discipline.Code",
            Operator = ConditionOperator.Equals,
            Value = ""
        };
        SelectedRule.Conditions.Add(condition);
        CurrentConditions.Add(new AclConditionViewModel(condition));
    }
    
    [RelayCommand]
    private void RemoveCondition()
    {
        if (SelectedRule == null || SelectedCondition == null) return;
        
        SelectedRule.Conditions.Remove(SelectedCondition.Condition);
        CurrentConditions.Remove(SelectedCondition);
        SelectedCondition = CurrentConditions.FirstOrDefault();
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
}

public partial class AclConditionViewModel : ObservableObject
{
    public AclCondition Condition { get; }
    
    public AclConditionViewModel(AclCondition condition)
    {
        Condition = condition;
    }
    
    public string AttributePath
    {
        get => Condition.AttributePath;
        set { Condition.AttributePath = value; OnPropertyChanged(); }
    }
    
    public ConditionOperator Operator
    {
        get => Condition.Operator;
        set { Condition.Operator = value; OnPropertyChanged(); }
    }
    
    public string Value
    {
        get => Condition.Value;
        set { Condition.Value = value; OnPropertyChanged(); }
    }
    
    public string DisplayText => Condition.ToString();
}
