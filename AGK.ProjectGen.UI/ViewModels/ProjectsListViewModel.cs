using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System.Collections.ObjectModel;
using System.Diagnostics;
using AGK.ProjectGen.Application.Interfaces;
using AGK.ProjectGen.Domain.Entities;

using System.IO;

namespace AGK.ProjectGen.UI.ViewModels;

public class NavigateToCreateProjectMessage { }

public class NavigateToEditProjectMessage 
{ 
    public Project Project { get; init; }
    public NavigateToEditProjectMessage(Project project) => Project = project;
}

public partial class ProjectsListViewModel : ObservableObject
{
    private readonly IProjectManagerService _projectManagerService;

    [ObservableProperty]
    private ObservableCollection<Project> _projects = new();

    [ObservableProperty]
    private Project? _selectedProject;

    public ProjectsListViewModel(IProjectManagerService projectManagerService)
    {
        _projectManagerService = projectManagerService;
        LoadProjectsCommand.Execute(null);
    }

    [RelayCommand]
    private async Task LoadProjects()
    {
        var list = await _projectManagerService.GetAllProjectsAsync();
        Projects = new ObservableCollection<Project>(list);
    }

    [RelayCommand]
    private void OpenProjectFolder()
    {
        var project = SelectedProject;
        if (project != null && Directory.Exists(project.RootPath))
        {
            Process.Start("explorer.exe", project.RootPath);
        }
    }
    
    [RelayCommand]
    private async Task DeleteProject()
    {
        if (SelectedProject == null) return;
        
        var result = System.Windows.MessageBox.Show(
            $"Вы уверены, что хотите удалить проект '{SelectedProject.Name}'? Файлы на диске останутся.", 
            "Подтверждение удаления", 
            System.Windows.MessageBoxButton.YesNo, 
            System.Windows.MessageBoxImage.Question);
            
        if (result == System.Windows.MessageBoxResult.Yes)
        {
             await _projectManagerService.DeleteProjectAsync(SelectedProject.Id);
             await LoadProjects();
        }
    }

    [RelayCommand]
    private void OpenProject()
    {
        if (SelectedProject != null)
        {
            WeakReferenceMessenger.Default.Send(new NavigateToEditProjectMessage(SelectedProject));
        }
    }

    [RelayCommand]
    private void CreateNewProject()
    {
        WeakReferenceMessenger.Default.Send(new NavigateToCreateProjectMessage());
    }
}
