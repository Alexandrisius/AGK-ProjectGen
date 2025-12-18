using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AGK.ProjectGen.Application.Interfaces;
using AGK.ProjectGen.Domain.AccessControl;

namespace AGK.ProjectGen.UI.ViewModels;

public partial class AclViewerViewModel : ObservableObject
{
    private readonly IAclService _aclService;

    [ObservableProperty]
    private string _path = string.Empty;

    [ObservableProperty]
    private ObservableCollection<AclRule> _aclRules = new();

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public AclViewerViewModel(IAclService aclService)
    {
        _aclService = aclService;
    }

    public void LoadAcl(string path)
    {
        Path = path;
        Refresh();
    }

    [RelayCommand]
    private void Refresh()
    {
        try
        {
            if (string.IsNullOrEmpty(Path) || !System.IO.Directory.Exists(Path))
            {
                StatusMessage = "Путь не существует";
                AclRules.Clear();
                return;
            }

            var rules = _aclService.GetDirectoryAcl(Path);
            AclRules = new ObservableCollection<AclRule>(rules);
            StatusMessage = $"Загружено {rules.Count} правил";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка: {ex.Message}";
            AclRules.Clear();
        }
    }
    
    /// <summary>
    /// Загружает превью ACL для папки, которая ещё не создана.
    /// Показывает правила из overrides + PlannedAcl.
    /// </summary>
    public void LoadPreview(string path, IEnumerable<AclRule> overrides, IEnumerable<AclRule>? plannedRules)
    {
        Path = path;
        AclRules.Clear();
        
        // Сначала добавляем overrides (имеют приоритет)
        foreach (var rule in overrides)
        {
            AclRules.Add(rule);
        }
        
        // Затем добавляем PlannedAcl из формул профиля
        if (plannedRules != null)
        {
            foreach (var rule in plannedRules)
            {
                // Не дублировать если уже есть в overrides
                if (!AclRules.Any(r => r.Identity == rule.Identity))
                {
                    AclRules.Add(rule);
                }
            }
        }
        
        StatusMessage = AclRules.Count > 0 
            ? $"Превью: {AclRules.Count} правил (папка ещё не создана)"
            : "Нет назначенных правил (папка ещё не создана)";
    }
}
