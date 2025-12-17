using AGK.ProjectGen.Domain.Entities;

namespace AGK.ProjectGen.Application.Interfaces;

public interface INamingEngine
{
    // Generates a name based on the formula and context
    string GenerateName(string formula, GeneratedNode nodeContext, Project project);
    
    // Applies formula with simple dictionary context (for preview recalculation)
    string ApplyFormula(string formula, Dictionary<string, string> context);
    
    // Validates if the name is safe for file system
    bool ValidateName(string name, out string error);
}
