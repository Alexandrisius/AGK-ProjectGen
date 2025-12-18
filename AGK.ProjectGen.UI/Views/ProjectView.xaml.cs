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
}
