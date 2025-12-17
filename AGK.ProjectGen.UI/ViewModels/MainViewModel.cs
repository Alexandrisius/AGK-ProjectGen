using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using AGK.ProjectGen.Domain.Entities;

namespace AGK.ProjectGen.UI.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IServiceProvider _serviceProvider;

    [ObservableProperty]
    private object? _currentView;

    public MainViewModel(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        NavigateToDashboard();
        
        WeakReferenceMessenger.Default.Register<MainViewModel, NavigateToCreateProjectMessage>(this, (r, m) =>
        {
            r.NavigateToCreateProject();
        });
        
        WeakReferenceMessenger.Default.Register<MainViewModel, NavigateToEditProjectMessage>(this, (r, m) =>
        {
            r.NavigateToEditProject(m.Project);
        });
    }

    [RelayCommand]
    public void NavigateToProfiles()
    {
        CurrentView = _serviceProvider.GetRequiredService<ProfileViewModel>();
    }

    [RelayCommand]
    public void NavigateToProjects()
    {
        CurrentView = _serviceProvider.GetRequiredService<ProjectsListViewModel>();
    }
    
    // Helper method to be called from Messenger
    public void NavigateToCreateProject()
    {
         CurrentView = _serviceProvider.GetRequiredService<ProjectViewModel>();
    }
    
    // Навигация к редактированию существующего проекта
    public void NavigateToEditProject(Project project)
    {
         var vm = _serviceProvider.GetRequiredService<ProjectViewModel>();
         vm.LoadExistingProject(project);
         CurrentView = vm;
    }
    
    public void NavigateToDashboard()
    {
        CurrentView = "Welcome to AGK ProjectGen";
    }
}
