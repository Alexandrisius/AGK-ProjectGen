using AGK.ProjectGen.Domain.Entities;
using AGK.ProjectGen.Domain.Schema;

namespace AGK.ProjectGen.Application.Interfaces;

public interface IGenerationService
{
    // Phase 1: In-Memory Preview Building
    // Returns the root of the generated tree structure
    GeneratedNode GenerateStructurePreview(Project project, ProfileSchema profile);
}
