using System.Collections.ObjectModel;

namespace AGK.ProjectGen.Domain.AccessControl;

/// <summary>
/// Пресет (шаблон) прав доступа для папки.
/// Содержит полный набор правил ACL, которые можно применить к любой папке в профиле.
/// </summary>
public class AclPreset
{
    /// <summary>
    /// Уникальный идентификатор пресета.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Название пресета (отображается в списке).
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Описание пресета (для чего предназначен).
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Дата создания пресета.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    /// <summary>
    /// Дата последнего изменения.
    /// </summary>
    public DateTime ModifiedAt { get; set; } = DateTime.Now;
    
    /// <summary>
    /// Правила ACL, входящие в пресет.
    /// При загрузке пресета эти правила копируются в целевую папку.
    /// </summary>
    public ObservableCollection<AclRuleDefinition> Rules { get; set; } = new();
    
    /// <summary>
    /// Создаёт глубокую копию пресета для применения к папке.
    /// </summary>
    public AclPreset Clone()
    {
        return new AclPreset
        {
            Id = Guid.NewGuid().ToString(),
            Name = Name,
            Description = Description,
            CreatedAt = DateTime.Now,
            ModifiedAt = DateTime.Now,
            Rules = new ObservableCollection<AclRuleDefinition>(
                Rules.Select(r => new AclRuleDefinition
                {
                    Id = Guid.NewGuid().ToString(),
                    PrincipalIdentity = r.PrincipalIdentity,
                    Rights = r.Rights,
                    IsDeny = r.IsDeny,
                    Description = r.Description,
                    Conditions = new List<AclCondition>(
                        r.Conditions.Select(c => new AclCondition
                        {
                            AttributePath = c.AttributePath,
                            Operator = c.Operator,
                            Value = c.Value
                        })
                    )
                })
            )
        };
    }
    
    /// <summary>
    /// Получает копии правил для применения к папке (не копирует ID пресета).
    /// </summary>
    public ObservableCollection<AclRuleDefinition> GetRulesCopy()
    {
        return new ObservableCollection<AclRuleDefinition>(
            Rules.Select(r => new AclRuleDefinition
            {
                Id = Guid.NewGuid().ToString(),
                PrincipalIdentity = r.PrincipalIdentity,
                Rights = r.Rights,
                IsDeny = r.IsDeny,
                Description = r.Description,
                Conditions = new List<AclCondition>(
                    r.Conditions.Select(c => new AclCondition
                    {
                        AttributePath = c.AttributePath,
                        Operator = c.Operator,
                        Value = c.Value
                    })
                )
            })
        );
    }
}
