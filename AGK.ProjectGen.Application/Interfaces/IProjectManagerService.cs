using AGK.ProjectGen.Domain.Entities;
using AGK.ProjectGen.Domain.Schema;

namespace AGK.ProjectGen.Application.Interfaces;

public interface IProjectManagerService
{
    Task<List<Project>> GetAllProjectsAsync();
    Task<Project?> GetProjectAsync(string id);
    
    // Generates the preview structure (in-memory)
    GeneratedNode GeneratePreview(Project project, ProfileSchema profile);
    
    // Orchestrates the actual creation: diff calculation, execution, and saving metadata
    Task CreateProjectAsync(Project project, ProfileSchema profile, GeneratedNode previewStructure);
    
    /// <summary>
    /// Получает план изменений (diff) для структуры проекта.
    /// </summary>
    List<GeneratedNode> GetDiffPlan(GeneratedNode previewStructure, string rootPath, ProfileSchema profile);
    
    /// <summary>
    /// Получает список папок с файлами, которые будут удалены.
    /// </summary>
    List<DeleteFolderInfo> GetFoldersWithFilesToDelete(List<GeneratedNode> diffPlan);
    
    /// <summary>
    /// Выполняет план изменений проекта (создание/удаление папок).
    /// </summary>
    Task ExecuteProjectPlanAsync(Project project, ProfileSchema profile, List<GeneratedNode> diffPlan);
    
    Task DeleteProjectAsync(string id); // For now, maybe metadata only? Or files too?
}
