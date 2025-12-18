using System.Windows;

namespace AGK.ProjectGen.UI.Views;

public partial class AclPresetDialog : Window
{
    public bool PresetLoaded { get; private set; }
    
    public AclPresetDialog()
    {
        InitializeComponent();
    }
    
    private void Load_Click(object sender, RoutedEventArgs e)
    {
        PresetLoaded = true;
        DialogResult = true;
        Close();
    }
    
    private void Close_Click(object sender, RoutedEventArgs e)
    {
        PresetLoaded = false;
        DialogResult = false;
        Close();
    }
}
