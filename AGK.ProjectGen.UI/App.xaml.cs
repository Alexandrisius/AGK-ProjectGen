using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using AGK.ProjectGen.Application.Interfaces;
using AGK.ProjectGen.Application.Services;
using AGK.ProjectGen.Infrastructure.Services;
using AGK.ProjectGen.Infrastructure.Repositories;
using AGK.ProjectGen.UI.ViewModels;
using AGK.ProjectGen.UI.Services;

namespace AGK.ProjectGen.UI;

public partial class App : System.Windows.Application
{
    public new static App Current => (App)System.Windows.Application.Current;
    public IServiceProvider Services { get; }

    public App()
    {
        Services = ConfigureServices();
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Infrastructure
        services.AddSingleton<IFileSystemService, FileSystemService>();
        services.AddSingleton<IAclService, AclService>();
        services.AddSingleton<IProfileRepository, AGK.ProjectGen.Infrastructure.Repositories.ProfileRepository>();
        services.AddSingleton<IProjectRepository, AGK.ProjectGen.Infrastructure.Repositories.JsonProjectRepository>();
        services.AddSingleton<IAppSettingsRepository, AGK.ProjectGen.Infrastructure.Repositories.AppSettingsRepository>();

        // Application
        services.AddSingleton<INamingEngine, NamingEngine>();
        services.AddSingleton<IGenerationService, StructureGenerator>();
        services.AddSingleton<IProjectUpdater, ProjectUpdater>();
        services.AddSingleton<IProjectManagerService, ProjectManagerService>();
        services.AddSingleton<IAclFormulaEngine, AclFormulaEngine>();
        services.AddSingleton<ISecurityPrincipalRepository, SecurityPrincipalRepository>();
        
        // UI Services
        services.AddSingleton<IDialogService, DialogService>();

        // ViewModels
        services.AddSingleton<MainViewModel>();
        services.AddTransient<ProfileViewModel>();
        services.AddTransient<ProjectViewModel>();
        services.AddTransient<ProjectsListViewModel>();

        return services.BuildServiceProvider();
    }
}

