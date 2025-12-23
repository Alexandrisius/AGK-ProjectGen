using System.Windows;
using System.Windows.Controls;
using AGK.ProjectGen.UI.ViewModels;

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
    
    private async void DataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
    {
        if (e.EditAction == DataGridEditAction.Commit)
        {
            // Сохраняем изменения после редактирования ячейки
            if (DataContext is SecurityPrincipalsViewModel vm)
            {
                // Небольшая задержка чтобы binding успел обновиться
                await System.Threading.Tasks.Task.Delay(100);
                await vm.SaveChangesAsync();
            }
        }
    }
}
