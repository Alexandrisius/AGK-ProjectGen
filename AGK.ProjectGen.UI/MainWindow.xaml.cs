using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using AGK.ProjectGen.UI.ViewModels;

namespace AGK.ProjectGen.UI;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = App.Current.Services.GetService<MainViewModel>();
    }
}