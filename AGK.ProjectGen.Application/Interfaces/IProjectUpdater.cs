using AGK.ProjectGen.Domain.Entities;
using AGK.ProjectGen.Domain.Schema;

namespace AGK.ProjectGen.Application.Interfaces;

public interface IProjectUpdater
{
    // Phase 2: Execution Plan
    // Compares current structure on disk (or previous state) with new generated structure
    // Returns a diff plan (list of nodes with operations)
    List<GeneratedNode> CreateDiffPlan(GeneratedNode newStructure, string rootPath, ProfileSchema profile);
    
    // Executes the plan
    Task ExecutePlanAsync(List<GeneratedNode> diffPlan, Project project, ProfileSchema profile);
    
    /// <summary>
    /// Получает информацию о папках с файлами, которые будут удалены.
    /// Используется для показа диалога подтверждения.
    /// </summary>
    List<DeleteFolderInfo> GetFoldersWithFilesToDelete(List<GeneratedNode> diffPlan);
}

/// <summary>
/// Информация о папке, которая будет удалена и содержит файлы.
/// </summary>
public class DeleteFolderInfo
{
    public string FolderPath { get; set; } = string.Empty;
    public string FolderName { get; set; } = string.Empty;
    public List<string> Files { get; set; } = new();
}
