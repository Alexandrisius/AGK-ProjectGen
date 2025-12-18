using AGK.ProjectGen.Domain.AccessControl;
using AGK.ProjectGen.Domain.Entities;
using AGK.ProjectGen.Domain.Schema;

namespace AGK.ProjectGen.Application.Interfaces;

public interface IAclFormulaEngine
{
    /// <summary>
    /// Вычисляет ACL правила для узла на основе его контекста и правил из определения структуры.
    /// </summary>
    List<AclRule> CalculateAclRules(GeneratedNode node, StructureNodeDefinition? nodeDef, ProfileSchema profile);
    
    /// <summary>
    /// Проверяет, выполняется ли условие для заданного контекста.
    /// </summary>
    bool EvaluateCondition(AclCondition condition, Dictionary<string, object> context);
    
    /// <summary>
    /// Проверяет, выполняются ли все условия (AND логика).
    /// </summary>
    bool EvaluateConditions(List<AclCondition> conditions, Dictionary<string, object> context);
}
