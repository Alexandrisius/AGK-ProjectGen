using AGK.ProjectGen.Domain.Schema;

namespace AGK.ProjectGen.Application.Interfaces;

public interface IProfileRepository
{
    Task<List<ProfileSchema>> GetAllAsync();
    Task<ProfileSchema?> GetByIdAsync(string id);
    Task SaveAsync(ProfileSchema profile);
    Task DeleteAsync(string id);
}
