using System.Windows.Controls;

namespace AGK.ProjectGen.UI.Views;

public partial class ProfileView : UserControl
{
    public ProfileView()
    {
        InitializeComponent();
    }

    private void OnStructureNodeSelected(object sender, System.Windows.RoutedPropertyChangedEventArgs<object> e)
    {
        if (DataContext is AGK.ProjectGen.UI.ViewModels.ProfileViewModel vm && e.NewValue is AGK.ProjectGen.Domain.Schema.StructureNodeDefinition node)
        {
            vm.SelectedStructureNode = node;
        }
        else if (DataContext is AGK.ProjectGen.UI.ViewModels.ProfileViewModel vm2)
        {
            vm2.SelectedStructureNode = null;
        }
    }
}
