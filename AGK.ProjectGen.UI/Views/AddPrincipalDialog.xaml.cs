using System.Windows;
using AGK.ProjectGen.Domain.AccessControl;
using AGK.ProjectGen.Domain.Enums;

namespace AGK.ProjectGen.UI.Views;

public partial class AddPrincipalDialog : Window
{
    private SecurityPrincipal? _result;
    
    public AddPrincipalDialog()
    {
        InitializeComponent();
        Loaded += (s, e) => NameTextBox.Focus();
    }
    
    public SecurityPrincipal? GetPrincipal() => _result;
    
    private void Add_Click(object sender, RoutedEventArgs e)
    {
        var name = NameTextBox.Text.Trim();
        
        if (string.IsNullOrEmpty(name))
        {
            MessageBox.Show("Введите имя группы или пользователя", "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        _result = new SecurityPrincipal
        {
            Name = name,
            Domain = string.IsNullOrWhiteSpace(DomainTextBox.Text) ? null : DomainTextBox.Text.Trim(),
            Type = GroupRadio.IsChecked == true ? SecurityPrincipalType.Group : SecurityPrincipalType.User,
            Description = $"Добавлено вручную"
        };
        
        DialogResult = true;
        Close();
    }
    
    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
