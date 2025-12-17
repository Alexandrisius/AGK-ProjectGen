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
}
