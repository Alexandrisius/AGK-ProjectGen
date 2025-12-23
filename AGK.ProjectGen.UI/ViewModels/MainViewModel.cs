using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using AGK.ProjectGen.Domain.Entities;
using AGK.ProjectGen.UI.Services;

namespace AGK.ProjectGen.UI.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IUpdateService _updateService;

    [ObservableProperty]
    private object? _currentView;
    
    [ObservableProperty]
    private string _currentVersion = "1.0.0";
    
    [ObservableProperty]
    private bool _isCheckingForUpdates;

    public MainViewModel(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _updateService = serviceProvider.GetRequiredService<IUpdateService>();
        
        // Получаем текущую версию
        CurrentVersion = _updateService.GetCurrentVersion() ?? "1.0.0";
        
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
        NavigateTo(_serviceProvider.GetRequiredService<ProfileViewModel>());
    }

    [RelayCommand]
    public void NavigateToProjects()
    {
        NavigateTo(_serviceProvider.GetRequiredService<ProjectsListViewModel>());
    }
    
    // Helper method to be called from Messenger
    public void NavigateToCreateProject()
    {
         NavigateTo(_serviceProvider.GetRequiredService<ProjectViewModel>());
    }
    
    // Навигация к редактированию существующего проекта
    public void NavigateToEditProject(Project project)
    {
         var vm = _serviceProvider.GetRequiredService<ProjectViewModel>();
         vm.LoadExistingProject(project);
         NavigateTo(vm);
    }
    
    public void NavigateToDashboard()
    {
        NavigateTo("Welcome to AGK ProjectGen");
    }

    private void NavigateTo(object newView)
    {
        // 1. Обработка ухода со страницы (Navigation Guard)
        if (CurrentView is ProjectViewModel projectVm)
        {
            projectVm.SaveCurrentStateAsDraft();
        }
        else if (CurrentView is ProfileViewModel profileVm)
        {
             if (profileVm.HasUnsavedChanges())
             {
                 var result = MessageBox.Show(
                     "Есть несохраненные изменения в профиле. Сохранить их перед выходом?", 
                     "Несохраненные изменения", 
                     MessageBoxButton.YesNoCancel,
                     MessageBoxImage.Warning);
                     
                 if (result == MessageBoxResult.Cancel) return;
                 
                 if (result == MessageBoxResult.Yes)
                 {
                     profileVm.SaveProfileCommand.Execute(null);
                 }
             }
        }

        CurrentView = newView;
    }
    
    [RelayCommand]
    private async Task CheckForUpdatesAsync()
    {
        if (IsCheckingForUpdates) return;
        
        IsCheckingForUpdates = true;
        
        try
        {
            var updateInfo = await _updateService.CheckForUpdatesAsync();
            
            if (updateInfo == null)
            {
                MessageBox.Show(
                    "У вас установлена последняя версия приложения.",
                    "Обновления",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }
            
            var result = MessageBox.Show(
                $"Доступна новая версия: {updateInfo.TargetFullRelease.Version}\n\n" +
                $"Текущая версия: {CurrentVersion}\n\n" +
                "Хотите обновить приложение сейчас?",
                "Доступно обновление",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                MessageBox.Show(
                    "Загрузка обновления началась.\nПриложение перезапустится автоматически.",
                    "Обновление",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                    
                await _updateService.DownloadAndApplyUpdateAsync();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Ошибка при проверке обновлений:\n{ex.Message}",
                "Ошибка",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            IsCheckingForUpdates = false;
        }
    }
}
