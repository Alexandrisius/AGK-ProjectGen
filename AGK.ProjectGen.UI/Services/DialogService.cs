using System.Windows;
using AGK.ProjectGen.Application.Interfaces;
using AGK.ProjectGen.UI.Views;

namespace AGK.ProjectGen.UI.Services;

/// <summary>
/// Реализация сервиса диалогов для WPF.
/// </summary>
public class DialogService : IDialogService
{
    public Task<bool> ShowDeleteConfirmationAsync(List<DeleteFolderInfo> foldersWithFiles)
    {
        if (foldersWithFiles == null || foldersWithFiles.Count == 0)
        {
            return Task.FromResult(true);
        }
        
        var dialog = new DeleteConfirmationDialog(foldersWithFiles);
        
        // Устанавливаем Owner только если MainWindow доступен
        if (System.Windows.Application.Current?.MainWindow != null)
        {
            dialog.Owner = System.Windows.Application.Current.MainWindow;
        }
        
        var result = dialog.ShowDialog() == true && dialog.IsConfirmed;
        return Task.FromResult(result);
    }
    
    public void ShowMessage(string title, string message)
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
    }
    
    public Task<bool> ShowConfirmationAsync(string title, string message)
    {
        var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
        return Task.FromResult(result == MessageBoxResult.Yes);
    }
}
