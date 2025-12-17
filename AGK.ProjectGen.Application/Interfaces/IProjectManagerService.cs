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
    
    Task DeleteProjectAsync(string id); // For now, maybe metadata only? Or files too?
}
