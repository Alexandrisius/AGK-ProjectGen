using AGK.ProjectGen.Application.Interfaces;

namespace AGK.ProjectGen.Application.Interfaces;

/// <summary>
/// Сервис для отображения диалогов пользователю.
/// </summary>
public interface IDialogService
{
    /// <summary>
    /// Показывает диалог подтверждения удаления папок с файлами.
    /// </summary>
    /// <param name="foldersWithFiles">Список папок с файлами для удаления.</param>
    /// <returns>true если пользователь подтвердил удаление, false если отменил.</returns>
    Task<bool> ShowDeleteConfirmationAsync(List<DeleteFolderInfo> foldersWithFiles);
    
    /// <summary>
    /// Показывает простой диалог с сообщением.
    /// </summary>
    void ShowMessage(string title, string message);
    
    /// <summary>
    /// Показывает диалог подтверждения.
    /// </summary>
    /// <returns>true если пользователь подтвердил, false если отменил.</returns>
    Task<bool> ShowConfirmationAsync(string title, string message);
}
