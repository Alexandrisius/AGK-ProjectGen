using System.Text.RegularExpressions;
using AGK.ProjectGen.Application.Interfaces;
using AGK.ProjectGen.Domain.Entities;
using AGK.ProjectGen.Domain.Enums;

namespace AGK.ProjectGen.Application.Services;

public class NamingEngine : INamingEngine
{
    // Regex for {Scope.Attribute}
    private static readonly Regex TokenRegex = new Regex(@"\{([\w\.]+)\}", RegexOptions.Compiled);

    public string GenerateName(string formula, GeneratedNode nodeContext, Project project)
    {
        if (string.IsNullOrWhiteSpace(formula))
            return nodeContext.Name; // Fallback to raw name if no formula

        // Evaluate tokens
        return TokenRegex.Replace(formula, match =>
        {
            string token = match.Groups[1].Value; // e.g. "Project.Code" or "Stage.Name"
            return ResolveToken(token, nodeContext, project);
        });
    }

    public string ApplyFormula(string formula, Dictionary<string, string> context)
    {
        if (string.IsNullOrWhiteSpace(formula))
            return string.Empty;

        return TokenRegex.Replace(formula, match =>
        {
            string token = match.Groups[1].Value;
            return context.TryGetValue(token, out var value) ? value : $"!{token}!";
        });
    }

    private string ResolveToken(string token, GeneratedNode node, Project project)
    {
        var parts = token.Split('.');
        if (parts.Length < 2) return token; // Invalid format, return as is? Or empty?

        string scope = parts[0];
        string attr = parts[1];

        // 1. Try Node Context (Specific attributes like Stage.Code where node IS a Stage)
        // In GeneratedNode, we store context in ContextAttributes. 
        // We also have node.NodeTypeId, but checking scope vs context keys is better.

        // Standard Scopes: Project, Parent, Node
        // Specific Scopes: Stage, Building, Discipline (found in ContextAttributes)

        if (scope.Equals("Project", StringComparison.OrdinalIgnoreCase))
        {
            if (attr.Equals("Name", StringComparison.OrdinalIgnoreCase)) return project.Name;
            if (attr.Equals("RootPath", StringComparison.OrdinalIgnoreCase)) return project.RootPath;
            if (attr.Equals("Id", StringComparison.OrdinalIgnoreCase)) return project.Id;
            
            if (project.AttributeValues.TryGetValue(attr, out var val))
                return FormatValue(val);
        }
        else if (scope.Equals("Node", StringComparison.OrdinalIgnoreCase))
        {
            // Usually refers to intrinsic properties or custom attributes of this specific node
            // For now, let's assume we look in ContextAttributes for "Node.<Attr>" or just matching keys?
            // "Node" is ambiguous if we don't map it. 
            // Let's assume ContextAttributes contains "Self.<Attr>" or we map it.
            // Simplified: Look in ContextAttributes for key 'attr' assuming it belongs to this node logic.
            // BUT, usually profile says "{Stage.Code}". 
            
            // If the node represents a specific entity (Stage, Building), it should have those in ContextAttributes.
        }
        
        // 2. Context Lookup (Stage, Building, etc.)
        // ContextAttributes keys: "Stage", "Building", "Discipline" -> Object (Dictionary or Wrapped)
        // If ContextAttributes["Stage"] is a Dictionary<string, object>, we look up attr there.
        
        if (node.ContextAttributes.TryGetValue(scope, out var contextObj))
        {
            if (contextObj is Dictionary<string, object> dict && dict.TryGetValue(attr, out var val))
            {
                return FormatValue(val);
            }
            if (contextObj is Dictionary<string, string> dictStr && dictStr.TryGetValue(attr, out var valStr))
            {
                return valStr;
            }
        }

        // 3. Parent Lookup (simple)
        if (scope.Equals("Parent", StringComparison.OrdinalIgnoreCase))
        {
            // Not easily accessible via GeneratedNode unless double linked or passed down.
            // GeneratedNode definition has Children, but Parent is useful.
            // Allow skipping for now or return empty.
        }

        return $"!{token}!"; // Error indicator
    }

    private string FormatValue(object? value)
    {
        if (value == null) return string.Empty;
        if (value is DateTime dt) return dt.ToString("yyyy-MM-dd");
        return value.ToString() ?? string.Empty;
    }

    public bool ValidateName(string name, out string error)
    {
        error = string.Empty;
        if (string.IsNullOrWhiteSpace(name))
        {
            error = "Name is empty";
            return false;
        }

        // Windows invalid chars
        char[] invalidChars = Path.GetInvalidFileNameChars();
        if (name.IndexOfAny(invalidChars) >= 0)
        {
            error = "Contains invalid characters";
            return false;
        }
        
        return true;
    }
}
