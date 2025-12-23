using System.Windows.Controls;
using AGK.ProjectGen.Domain.Entities;
using AGK.ProjectGen.UI.ViewModels;

namespace AGK.ProjectGen.UI.Views;

public partial class ProjectView : UserControl
{
    public ProjectView()
    {
        InitializeComponent();
    }
    
    private void PreviewTreeView_SelectedItemChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<object> e)
    {
        if (DataContext is ProjectViewModel vm && e.NewValue is GeneratedNode node)
        {
            vm.SelectedNode = node;
        }
    }
    
    private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Только обрабатываем события от главного TabControl, а не вложенных контролов
        if (e.Source is TabControl tabControl && tabControl.SelectedIndex == 2)
        {
            // Вкладка "③ Превью и создание" выбрана
            if (DataContext is ProjectViewModel vm && vm.GeneratePreviewCommand.CanExecute(null))
            {
                vm.GeneratePreviewCommand.Execute(null);
            }
        }
    }
}
