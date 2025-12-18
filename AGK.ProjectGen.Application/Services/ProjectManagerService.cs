using AGK.ProjectGen.Application.Interfaces;
using AGK.ProjectGen.Domain.Entities;
using AGK.ProjectGen.Domain.Schema;

namespace AGK.ProjectGen.Application.Services;

public class ProjectManagerService : IProjectManagerService
{
    private readonly IProjectRepository _projectRepository;
    private readonly IGenerationService _generationService;
    private readonly IProjectUpdater _projectUpdater;

    public ProjectManagerService(
        IProjectRepository projectRepository,
        IGenerationService generationService,
        IProjectUpdater projectUpdater)
    {
        _projectRepository = projectRepository;
        _generationService = generationService;
        _projectUpdater = projectUpdater;
    }

    public Task<List<Project>> GetAllProjectsAsync()
    {
        return _projectRepository.GetAllAsync();
    }

    public Task<Project?> GetProjectAsync(string id)
    {
        return _projectRepository.GetByIdAsync(id);
    }

    public GeneratedNode GeneratePreview(Project project, ProfileSchema profile)
    {
        return _generationService.GenerateStructurePreview(project, profile);
    }

    public List<GeneratedNode> GetDiffPlan(GeneratedNode previewStructure, string rootPath, ProfileSchema profile)
    {
        return _projectUpdater.CreateDiffPlan(previewStructure, rootPath, profile);
    }
    
    public List<DeleteFolderInfo> GetFoldersWithFilesToDelete(List<GeneratedNode> diffPlan)
    {
        return _projectUpdater.GetFoldersWithFilesToDelete(diffPlan);
    }
    
    public async Task ExecuteProjectPlanAsync(Project project, ProfileSchema profile, List<GeneratedNode> diffPlan)
    {
        // 1. Execute Plan (Create/Delete folders, set ACLs)
        await _projectUpdater.ExecutePlanAsync(diffPlan, project, profile);

        // 2. Update Project State
        project.State.LastGenerated = DateTime.Now;
        project.State.IsDirty = false;

        // 3. Persist Project Metadata
        await _projectRepository.SaveAsync(project);
    }

    public async Task CreateProjectAsync(Project project, ProfileSchema profile, GeneratedNode previewStructure)
    {
        // 1. Calculate Diff Plan (Execution Plan)
        var diffPlan = _projectUpdater.CreateDiffPlan(previewStructure, project.RootPath, profile);

        // 2. Execute Plan (Create folders, set ACLs)
        await _projectUpdater.ExecutePlanAsync(diffPlan, project, profile);

        // 3. Update Project State
        project.State.LastGenerated = DateTime.Now;
        project.State.IsDirty = false;

        // 4. Persist Project Metadata
        await _projectRepository.SaveAsync(project);
    }

    public async Task DeleteProjectAsync(string id)
    {
        await _projectRepository.DeleteAsync(id);
    }
}
