using System.Windows;

namespace AGK.ProjectGen.UI.Views;

public partial class SecurityPrincipalsDialog : Window
{
    public SecurityPrincipalsDialog()
    {
        InitializeComponent();
    }
    
    private void Close_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}
