using System.Windows;

namespace AGK.ProjectGen.UI.Views;

/// <summary>
/// Универсальный диалог подтверждения удаления сущности.
/// </summary>
public partial class ConfirmDeleteDialog : Window
{
    /// <summary>
    /// Результат диалога: true если пользователь подтвердил удаление.
    /// </summary>
    public bool IsConfirmed { get; private set; }
    
    /// <summary>
    /// Создаёт диалог подтверждения удаления.
    /// </summary>
    /// <param name="entityType">Тип сущности (например: "атрибут", "словарь")</param>
    /// <param name="entityName">Название сущности</param>
    public ConfirmDeleteDialog(string entityType, string entityName)
    {
        InitializeComponent();
        MessageText.Text = $"Вы уверены, что хотите удалить {entityType} «{entityName}»?";
    }
    
    /// <summary>
    /// Показывает диалог подтверждения удаления.
    /// </summary>
    /// <param name="entityType">Тип сущности (например: "атрибут", "словарь")</param>
    /// <param name="entityName">Название сущности</param>
    /// <returns>true если пользователь подтвердил удаление</returns>
    public static bool Show(string entityType, string entityName)
    {
        var dialog = new ConfirmDeleteDialog(entityType, entityName);
        dialog.Owner = System.Windows.Application.Current.MainWindow;
        dialog.ShowDialog();
        return dialog.IsConfirmed;
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
