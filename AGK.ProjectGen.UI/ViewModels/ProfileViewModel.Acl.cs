using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using AGK.ProjectGen.Application.Interfaces;
using AGK.ProjectGen.UI.Views;

namespace AGK.ProjectGen.UI.ViewModels;

// ACL-related functionality for ProfileViewModel
public partial class ProfileViewModel
{
    [RelayCommand]
    private void ConfigureAcl()
    {
        if (SelectedStructureNode == null || SelectedProfile == null) return;
        
        // Получаем репозиторий из DI контейнера
        var repository = App.Current.Services.GetService<ISecurityPrincipalRepository>();
        
        if (repository == null)
        {
            System.Windows.MessageBox.Show("Сервис безопасности не доступен", "Ошибка", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            return;
        }
        
        var viewModel = new AclRuleEditorViewModel(repository);
        viewModel.LoadRules(SelectedStructureNode.AclRules, $"Узел: {SelectedStructureNode.NodeTypeId}");
        
        // Загружаем словари и атрибуты профиля для динамического списка
        viewModel.LoadAttributes(SelectedProfile.Dictionaries, SelectedProfile.ProjectAttributes);
        
        var dialog = new AclRuleEditorDialog
        {
            DataContext = viewModel,
            Owner = System.Windows.Application.Current.MainWindow
        };
        
        if (dialog.ShowDialog() == true && dialog.DialogResultOk)
        {
            // Правила уже изменены в коллекции SelectedStructureNode.AclRules
            // т.к. мы передали ссылку на коллекцию
        }
    }
}

