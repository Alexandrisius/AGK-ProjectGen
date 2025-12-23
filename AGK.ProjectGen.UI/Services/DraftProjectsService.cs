using System.Collections.Concurrent;
using AGK.ProjectGen.Domain.Entities;

namespace AGK.ProjectGen.UI.Services;

public class DraftProjectsService : IDraftProjectsService
{
    // Храним черновики в памяти: ProfileId -> Project
    private readonly ConcurrentDictionary<string, Project> _drafts = new();

    public void SaveDraft(Project project)
    {
        if (project == null || string.IsNullOrEmpty(project.ProfileId)) return;
        
        // Клонируем проект или сохраняем ссылку? 
        // Для простоты пока сохраняем ссылку, так как ProjectViewModel создает новый инстанс при уходе
        // Но лучше было бы делать глубокую копию, если мы хотим избежать мутаций.
        // Учитывая, что Project - это POCO, и мы его полность восстанавливаем в VM,
        // сохранение ссылки на объект, который VM "бросает", допустимо.
        
        _drafts[project.ProfileId] = project;
    }

    public Project? GetDraft(string profileId)
    {
        if (string.IsNullOrEmpty(profileId)) return null;
        
        if (_drafts.TryGetValue(profileId, out var project))
        {
            return project;
        }
        return null;
    }

    public bool HasDraft(string profileId)
    {
        if (string.IsNullOrEmpty(profileId)) return false;
        return _drafts.ContainsKey(profileId);
    }

    public void ClearDraft(string profileId)
    {
        if (string.IsNullOrEmpty(profileId)) return;
        _drafts.TryRemove(profileId, out _);
    }
}
