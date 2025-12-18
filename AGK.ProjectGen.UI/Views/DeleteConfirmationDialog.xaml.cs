using System.Windows;
using AGK.ProjectGen.Application.Interfaces;

namespace AGK.ProjectGen.UI.Views;

public partial class DeleteConfirmationDialog : Window
{
    public bool IsConfirmed { get; private set; }
    
    public DeleteConfirmationDialog(List<DeleteFolderInfo> foldersWithFiles)
    {
        InitializeComponent();
        
        // Подготавливаем данные для отображения
        var displayItems = foldersWithFiles.Select(f => new FolderDisplayItem
        {
            FolderName = f.FolderName,
            FolderPath = f.FolderPath,
            Files = f.Files.Select(path => System.IO.Path.GetFileName(path)).ToList()
        }).ToList();
        
        FoldersTreeView.ItemsSource = displayItems;
        
        // Подсчитываем статистику
        var totalFolders = foldersWithFiles.Count;
        var totalFiles = foldersWithFiles.Sum(f => f.Files.Count);
        SummaryText.Text = $"Будет удалено {totalFolders} папок, содержащих {totalFiles} файлов.";
    }
    
    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        IsConfirmed = true;
        DialogResult = true;
        Close();
    }
    
    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        IsConfirmed = false;
        DialogResult = false;
        Close();
    }
}

/// <summary>
/// Вспомогательный класс для отображения папки в TreeView.
/// </summary>
public class FolderDisplayItem
{
    public string FolderName { get; set; } = string.Empty;
    public string FolderPath { get; set; } = string.Empty;
    public List<string> Files { get; set; } = new();
}
