using System.Windows;

namespace AGK.ProjectGen.UI.Views;

public partial class InputDialog : Window
{
    public string InputValue { get; private set; } = string.Empty;
    
    public InputDialog(string title, string prompt, string defaultValue = "")
    {
        InitializeComponent();
        
        Title = title;
        PromptText.Text = prompt;
        InputTextBox.Text = defaultValue;
        
        Loaded += (s, e) =>
        {
            InputTextBox.Focus();
            InputTextBox.SelectAll();
        };
    }
    
    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        InputValue = InputTextBox.Text;
        DialogResult = true;
        Close();
    }
    
    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
