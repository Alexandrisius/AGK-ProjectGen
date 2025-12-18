using System.Windows;
using AGK.ProjectGen.UI.ViewModels;

namespace AGK.ProjectGen.UI.Views;

public partial class AclAssignDialog : Window
{
    public bool DialogResultOk { get; private set; }
    
    public AclAssignDialog()
    {
        InitializeComponent();
    }
    
    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        DialogResultOk = true;
        if (DataContext is AclAssignViewModel vm)
        {
            vm.Confirm();
        }
        DialogResult = true;
        Close();
    }
    
    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResultOk = false;
        if (DataContext is AclAssignViewModel vm)
        {
            vm.Cancel();
        }
        DialogResult = false;
        Close();
    }
}
