using AGK.ProjectGen.Domain.Entities;

namespace AGK.ProjectGen.Application.Interfaces;

public interface IProjectRepository
{
    Task<List<Project>> GetAllAsync();
    Task<Project?> GetByIdAsync(string id);
    Task SaveAsync(Project project);
    Task DeleteAsync(string id);
}
