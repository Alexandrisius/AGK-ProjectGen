using AGK.ProjectGen.Domain.Entities;

namespace AGK.ProjectGen.UI.Services;

public interface IDraftProjectsService
{
    void SaveDraft(Project project);
    Project? GetDraft(string profileId);
    bool HasDraft(string profileId);
    void ClearDraft(string profileId);
}
