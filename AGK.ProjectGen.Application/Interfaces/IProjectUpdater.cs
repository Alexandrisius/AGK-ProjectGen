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
}
