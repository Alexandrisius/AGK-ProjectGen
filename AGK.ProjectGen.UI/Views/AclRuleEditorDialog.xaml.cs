using System.Windows;

namespace AGK.ProjectGen.UI.Views;

public partial class AclRuleEditorDialog : Window
{
    public bool DialogResultOk { get; private set; }
    
    public AclRuleEditorDialog()
    {
        InitializeComponent();
    }
    
    private void Save_Click(object sender, RoutedEventArgs e)
    {
        DialogResultOk = true;
        DialogResult = true;
        Close();
    }
    
    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResultOk = false;
        DialogResult = false;
        Close();
    }
}
