using System.Windows;

namespace AGK.ProjectGen.UI.Views;

public partial class AclViewerDialog : Window
{
    public AclViewerDialog()
    {
        InitializeComponent();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
