using AGK.ProjectGen.Application.Interfaces;
using AGK.ProjectGen.Domain.AccessControl;
using AGK.ProjectGen.Domain.Entities;
using AGK.ProjectGen.Domain.Enums;
using AGK.ProjectGen.Domain.Schema;

namespace AGK.ProjectGen.Application.Services;

public class AclFormulaEngine : IAclFormulaEngine
{
    public List<AclRule> CalculateAclRules(GeneratedNode node, StructureNodeDefinition? nodeDef, ProfileSchema profile)
    {
        var rules = new List<AclRule>();
        
        // 1. Применяем правила из определения узла в структуре (если есть)
        if (nodeDef?.AclRules != null)
        {
            foreach (var ruleDef in nodeDef.AclRules)
            {
                if (EvaluateConditions(ruleDef.Conditions, node.ContextAttributes))
                {
                    rules.Add(new AclRule
                    {
                        Identity = ruleDef.PrincipalIdentity,
                        Rights = ruleDef.Rights,
                        IsDeny = ruleDef.IsDeny,
                        Competence = ruleDef.Description ?? string.Empty
                    });
                }
            }
        }
        
        // 2. Применяем правила из AclBindings профиля (старая система)
        var bindings = profile.AclBindings
            .Where(b => b.NodeTypeId == node.NodeTypeId)
            .ToList();
        
        foreach (var binding in bindings)
        {
            if (binding.Conditions.All(c => EvaluateConditionLegacy(c, node.ContextAttributes)))
            {
                var template = profile.AclTemplates.FirstOrDefault(t => t.Id == binding.TemplateId);
                if (template != null)
                {
                    rules.AddRange(template.Rules);
                }
            }
        }
        
        // 3. Per-node overrides имеют приоритет
        if (node.NodeAclOverrides.Count > 0)
        {
            // Overrides полностью заменяют формульные правила
            return node.NodeAclOverrides.ToList();
        }
        
        return rules;
    }
    
    public bool EvaluateCondition(AclCondition condition, Dictionary<string, object> context)
    {
        var value = ResolveAttributePath(condition.AttributePath, context);
        if (value == null) return false;
        
        var strValue = value.ToString() ?? string.Empty;
        var compareValue = condition.Value;
        
        return condition.Operator switch
        {
            ConditionOperator.Equals => strValue.Equals(compareValue, StringComparison.OrdinalIgnoreCase),
            ConditionOperator.NotEquals => !strValue.Equals(compareValue, StringComparison.OrdinalIgnoreCase),
            ConditionOperator.Contains => strValue.Contains(compareValue, StringComparison.OrdinalIgnoreCase),
            ConditionOperator.NotContains => !strValue.Contains(compareValue, StringComparison.OrdinalIgnoreCase),
            ConditionOperator.GreaterThan => CompareNumeric(strValue, compareValue) > 0,
            ConditionOperator.LessThan => CompareNumeric(strValue, compareValue) < 0,
            ConditionOperator.GreaterThanOrEqual => CompareNumeric(strValue, compareValue) >= 0,
            ConditionOperator.LessThanOrEqual => CompareNumeric(strValue, compareValue) <= 0,
            _ => false
        };
    }
    
    public bool EvaluateConditions(List<AclCondition> conditions, Dictionary<string, object> context)
    {
        if (conditions.Count == 0) return true;
        return conditions.All(c => EvaluateCondition(c, context));
    }
    
    /// <summary>
    /// Совместимость со старым форматом AclCondition (где Operator - строка).
    /// </summary>
    private bool EvaluateConditionLegacy(AclCondition condition, Dictionary<string, object> context)
    {
        return EvaluateCondition(condition, context);
    }
    
    private object? ResolveAttributePath(string path, Dictionary<string, object> context)
    {
        var parts = path.Split('.');
        if (parts.Length < 2) return null;
        
        var scope = parts[0];
        var attr = parts[1];
        
        if (context.TryGetValue(scope, out var scopeObj))
        {
            if (scopeObj is Dictionary<string, object> dict && dict.TryGetValue(attr, out var val))
            {
                return val;
            }
            if (scopeObj is Dictionary<string, string> dictStr && dictStr.TryGetValue(attr, out var valStr))
            {
                return valStr;
            }
        }
        
        // Попробуем найти как плоский ключ "Scope.Attr"
        if (context.TryGetValue(path, out var flatVal))
        {
            return flatVal;
        }
        
        return null;
    }
    
    private int CompareNumeric(string a, string b)
    {
        if (double.TryParse(a, out var numA) && double.TryParse(b, out var numB))
        {
            return numA.CompareTo(numB);
        }
        return string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
    }
}
