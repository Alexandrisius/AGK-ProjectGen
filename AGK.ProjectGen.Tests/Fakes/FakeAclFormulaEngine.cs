using AGK.ProjectGen.Application.Interfaces;
using AGK.ProjectGen.Domain.AccessControl;
using AGK.ProjectGen.Domain.Entities;
using AGK.ProjectGen.Domain.Schema;

namespace AGK.ProjectGen.Tests.Fakes;

public class FakeAclFormulaEngine : IAclFormulaEngine
{
    public List<AclRule> CalculateAclRules(GeneratedNode node, StructureNodeDefinition? nodeDef, ProfileSchema profile)
    {
        return new List<AclRule>();
    }

    public bool EvaluateCondition(AclCondition condition, Dictionary<string, object> context)
    {
        return true;
    }

    public bool EvaluateConditions(List<AclCondition> conditions, Dictionary<string, object> context)
    {
        return conditions.Count == 0 || conditions.All(c => EvaluateCondition(c, context));
    }
}
