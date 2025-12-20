using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System.Collections.ObjectModel;
using System.Diagnostics;
using AGK.ProjectGen.Application.Interfaces;
using AGK.ProjectGen.Domain.Entities;
using AGK.ProjectGen.Domain.Schema;

using System.IO;

namespace AGK.ProjectGen.UI.ViewModels;

public class NavigateToCreateProjectMessage { }

public class NavigateToEditProjectMessage 
{ 
    public Project Project { get; init; }
    public NavigateToEditProjectMessage(Project project) => Project = project;
}

/// <summary>
/// Динамический столбец для отображения атрибутов проекта
/// </summary>
public class ProjectColumnDefinition
{
    public string Key { get; set; } = string.Empty;
    public string Header { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public bool IsSystemColumn { get; set; }
    public int DisplayIndex { get; set; }
    public double Width { get; set; } = 150;
    public bool IsVisible { get; set; } = true;
    public string BindingPath { get; set; } = string.Empty;
}

/// <summary>
/// Модель строки проекта с динамическими атрибутами
/// </summary>
public class ProjectRowViewModel : ObservableObject
{
    public Project Project { get; }
    public Dictionary<string, object?> AttributeValues { get; } = new();
    
    public string Id => Project.Id;
    public string Name => Project.Name;
    public string RootPath => Project.RootPath;
    public DateTime LastGenerated => Project.State.LastGenerated;
    public string ProfileName { get; set; } = string.Empty;
    
    public ProjectRowViewModel(Project project, ProfileSchema? profile)
    {
        Project = project;
        
        if (profile != null)
        {
            ProfileName = profile.Name;
            
            // Заполняем атрибуты из проекта
            foreach (var attr in profile.ProjectAttributes.Where(a => a.IsProjectAttribute))
            {
                object? value = null;
                project.AttributeValues?.TryGetValue(attr.Key, out value);
                AttributeValues[attr.Key] = value;
            }
        }
    }
    
    /// <summary>
    /// Получает значение атрибута по ключу
    /// </summary>
    public object? GetAttributeValue(string key)
    {
        AttributeValues.TryGetValue(key, out var value);
        return value ?? "—";
    }
}

public partial class ProjectsListViewModel : ObservableObject
{
    private readonly IProjectManagerService _projectManagerService;
    private readonly IProfileRepository _profileRepository;
    private readonly IAppSettingsRepository _appSettings;
    
    private const string VIEW_NAME = "ProjectsList";

    [ObservableProperty]
    private ObservableCollection<Project> _projects = new();
    
    [ObservableProperty]
    private ObservableCollection<ProjectRowViewModel> _projectRows = new();
    
    [ObservableProperty]
    private ObservableCollection<ProjectColumnDefinition> _columnDefinitions = new();

    [ObservableProperty]
    private ProjectRowViewModel? _selectedProjectRow;

    [ObservableProperty]
    private Project? _selectedProject;

    private List<ProfileSchema> _profiles = new();

    public ProjectsListViewModel(
        IProjectManagerService projectManagerService,
        IProfileRepository profileRepository,
        IAppSettingsRepository appSettings)
    {
        _projectManagerService = projectManagerService;
        _profileRepository = profileRepository;
        _appSettings = appSettings;
        LoadProjectsCommand.Execute(null);
    }

    [RelayCommand]
    private async Task LoadProjects()
    {
        // Загружаем профили
        _profiles = await _profileRepository.GetAllAsync();
        
        // Загружаем проекты
        var list = await _projectManagerService.GetAllProjectsAsync();
        Projects = new ObservableCollection<Project>(list);
        
        // Генерируем определения столбцов
        await GenerateColumnDefinitionsAsync();
        
        // Создаём строки с атрибутами
        var rows = new ObservableCollection<ProjectRowViewModel>();
        foreach (var project in list)
        {
            var profile = _profiles.FirstOrDefault(p => p.Id == project.ProfileId);
            rows.Add(new ProjectRowViewModel(project, profile));
        }
        ProjectRows = rows;
    }

    private async Task GenerateColumnDefinitionsAsync()
    {
        var columns = new List<ProjectColumnDefinition>();
        
        // Системные столбцы
        // Системные столбцы

        
        columns.Add(new ProjectColumnDefinition
        {
            Key = "ProfileName",
            Header = "ПРОФИЛЬ",
            IsSystemColumn = true,
            IsRequired = false,
            DisplayIndex = 1,
            Width = 150,
            BindingPath = "ProfileName"
        });
        
        // Сбор всех уникальных атрибутов из всех профилей
        var allAttributes = new Dictionary<string, FieldSchema>();
        var requiredAttributes = new HashSet<string>();
        
        foreach (var profile in _profiles)
        {
            foreach (var attr in profile.ProjectAttributes.Where(a => a.IsProjectAttribute))
            {
                if (!allAttributes.ContainsKey(attr.Key))
                {
                    allAttributes[attr.Key] = attr;
                }
                
                if (attr.IsRequired)
                {
                    requiredAttributes.Add(attr.Key);
                }
            }
        }
        
        // Добавляем атрибутные столбцы
        int idx = 2;
        foreach (var attr in allAttributes.Values.OrderBy(a => a.Order))
        {
            columns.Add(new ProjectColumnDefinition
            {
                Key = attr.Key,
                Header = attr.DisplayName.ToUpperInvariant(),
                IsSystemColumn = false,
                IsRequired = requiredAttributes.Contains(attr.Key),
                DisplayIndex = idx++,
                Width = 150,
                BindingPath = $"[{attr.Key}]"
            });
        }
        
        // Системные столбцы в конце
        columns.Add(new ProjectColumnDefinition
        {
            Key = "RootPath",
            Header = "ПУТЬ",
            IsSystemColumn = true,
            IsRequired = false,
            DisplayIndex = idx++,
            Width = 250,
            BindingPath = "RootPath"
        });
        
        columns.Add(new ProjectColumnDefinition
        {
            Key = "LastGenerated",
            Header = "ИЗМЕНЁН",
            IsSystemColumn = true,
            IsRequired = false,
            DisplayIndex = idx++,
            Width = 130,
            BindingPath = "LastGenerated"
        });
        
        // Загружаем сохранённые настройки
        var savedSettings = await _appSettings.GetColumnsSettingsAsync(VIEW_NAME);
        if (savedSettings != null)
        {
            foreach (var savedCol in savedSettings.Columns)
            {
                var col = columns.FirstOrDefault(c => c.Key == savedCol.Key);
                if (col != null)
                {
                    col.DisplayIndex = savedCol.DisplayIndex;
                    col.Width = savedCol.Width;
                    col.IsVisible = savedCol.IsVisible;
                }
            }
            
            // Сортируем по сохранённому DisplayIndex
            columns = columns.OrderBy(c => c.DisplayIndex).ToList();
        }
        
        ColumnDefinitions = new ObservableCollection<ProjectColumnDefinition>(columns);
    }

    public async Task SaveColumnSettingsAsync()
    {
        var settings = new ColumnsSettingsConfig
        {
            Columns = ColumnDefinitions.Select(c => new ColumnSetting
            {
                Key = c.Key,
                DisplayIndex = c.DisplayIndex,
                Width = c.Width,
                IsVisible = c.IsVisible
            }).ToList()
        };
        
        await _appSettings.SaveColumnsSettingsAsync(VIEW_NAME, settings);
    }

    public void UpdateColumnOrder(string columnKey, int newDisplayIndex)
    {
        var column = ColumnDefinitions.FirstOrDefault(c => c.Key == columnKey);
        if (column != null)
        {
            column.DisplayIndex = newDisplayIndex;
        }
    }
    
    public void UpdateColumnWidth(string columnKey, double newWidth)
    {
        var column = ColumnDefinitions.FirstOrDefault(c => c.Key == columnKey);
        if (column != null)
        {
            column.Width = newWidth;
        }
    }

    [RelayCommand]
    private void OpenProjectFolder()
    {
        var project = SelectedProjectRow?.Project ?? SelectedProject;
        if (project != null && Directory.Exists(project.RootPath))
        {
            Process.Start("explorer.exe", project.RootPath);
        }
    }
    
    [RelayCommand]
    private async Task DeleteProject()
    {
        var project = SelectedProjectRow?.Project ?? SelectedProject;
        if (project == null) return;
        
        var result = System.Windows.MessageBox.Show(
            $"Вы уверены, что хотите удалить проект '{project.Name}'? Файлы на диске останутся.", 
            "Подтверждение удаления", 
            System.Windows.MessageBoxButton.YesNo, 
            System.Windows.MessageBoxImage.Question);
            
        if (result == System.Windows.MessageBoxResult.Yes)
        {
             await _projectManagerService.DeleteProjectAsync(project.Id);
             await LoadProjects();
        }
    }

    [RelayCommand]
    private void OpenProject()
    {
        var project = SelectedProjectRow?.Project ?? SelectedProject;
        if (project != null)
        {
            WeakReferenceMessenger.Default.Send(new NavigateToEditProjectMessage(project));
        }
    }

    [RelayCommand]
    private void CreateNewProject()
    {
        WeakReferenceMessenger.Default.Send(new NavigateToCreateProjectMessage());
    }
}
