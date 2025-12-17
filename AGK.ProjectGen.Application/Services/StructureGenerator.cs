using AGK.ProjectGen.Application.Interfaces;
using AGK.ProjectGen.Domain.Entities;
using AGK.ProjectGen.Domain.Enums;
using AGK.ProjectGen.Domain.Schema;

namespace AGK.ProjectGen.Application.Services;

public class StructureGenerator : IGenerationService
{
    private readonly INamingEngine _namingEngine;

    public StructureGenerator(INamingEngine namingEngine)
    {
        _namingEngine = namingEngine;
    }

    public GeneratedNode GenerateStructurePreview(Project project, ProfileSchema profile)
    {
        var rootNode = new GeneratedNode
        {
            NodeTypeId = "ProjectRoot",
            Name = project.RootPath, // Or use formula for root if needed
            FullPath = project.RootPath, // Should be absolute
            Exists = true, // Assumed for root or checked later
            Operation = NodeOperation.None
        };

        // Root context usually contains Project Attributes?
        // Project attributes are separate in Project object, accessible via {Project.X}.
        // But we can put them in ContextAttributes if generic access is needed.
        rootNode.ContextAttributes = new Dictionary<string, object>(); 

        // Recursively generate children
        foreach (var structureDef in profile.Structure.RootNodes)
        {
            var children = ProcessNodeDefinition(structureDef, rootNode, project, profile);
            rootNode.Children.AddRange(children);
        }

        return rootNode;
    }

    private List<GeneratedNode> ProcessNodeDefinition(
        StructureNodeDefinition def, 
        GeneratedNode parent, 
        Project project, 
        ProfileSchema profile)
    {
        var result = new List<GeneratedNode>();

        // 1. Check Condition
        if (!EvaluateCondition(def.ConditionExpression, parent.ContextAttributes, project))
            return result;

        // 2. Determine Items (Multiplicity)
        var items = ResolveSourceItems(def, project, profile);

        // 3. Create Nodes
        foreach (var itemContext in items)
        {
            // Find NodeType definition
            var nodeType = profile.NodeTypes.FirstOrDefault(nt => nt.TypeId == def.NodeTypeId);
            if (nodeType == null) continue; // Configuration error

            // Fix Context: Parent + Item
            var currentContext = new Dictionary<string, object>(parent.ContextAttributes);
            foreach (var kvp in itemContext)
            {
                currentContext[kvp.Key] = kvp.Value;
            }

            // Create Node
            var newNode = new GeneratedNode
            {
                NodeTypeId = def.NodeTypeId,
                ContextAttributes = currentContext
            };

            // Calculate Name
            string formula = def.NamingFormulaOverride ?? nodeType.DefaultFormula ?? string.Empty;

            newNode.Name = _namingEngine.GenerateName(formula, newNode, project);
            newNode.NameFormula = formula; // Store formula for recalculation
            
            // Build Path
            newNode.FullPath = Path.Combine(parent.FullPath, newNode.Name);
            newNode.Operation = NodeOperation.Create; // Default, will be refined by DiffService

            // Recurse children
            foreach (var childDef in def.Children)
            {
                var grandChildren = ProcessNodeDefinition(childDef, newNode, project, profile);
                newNode.Children.AddRange(grandChildren);
            }

            result.Add(newNode);
        }

        return result;
    }

    private List<Dictionary<string, object>> ResolveSourceItems(
        StructureNodeDefinition def, 
        Project project,
        ProfileSchema profile)
    {
        var list = new List<Dictionary<string, object>>();

        switch (def.Multiplicity)
        {
            case MultiplicitySource.Single:
                // Single item, empty context or default context
                list.Add(new Dictionary<string, object>());
                break;

            case MultiplicitySource.Dictionary:
                // SourceKey = Dictionary Name (e.g. "Stages")
                // Project.CompositionSelections["Stages"] = ["P", "R"] (List of codes)
                if (!string.IsNullOrEmpty(def.SourceKey) && 
                    project.CompositionSelections.TryGetValue(def.SourceKey, out var selectedCodes))
                {
                    // Find Dictionary Schema
                    var dictSchema = profile.Dictionaries.FirstOrDefault(d => d.Key == def.SourceKey);
                    if (dictSchema != null)
                    {
                        foreach (var code in selectedCodes)
                        {
                            var itemDef = dictSchema.Items.FirstOrDefault(i => i.Code == code);
                            if (itemDef != null)
                            {
                                // Context: Scope = DictionaryName (e.g. "Stage")
                                // Attributes: Code, Name, Metadata
                                var itemCtx = new Dictionary<string, object>
                                {
                                    { "Code", itemDef.Code },
                                    { "Name", itemDef.Name }
                                };
                                // Flatten metadata? Or keep in dictionary?
                                // Let's simplify: Context["Stage"] = new Dict { Code=..., Name=... }
                                var scopeName = RemovePlural(def.SourceKey); // Heuristic or explicit?
                                // Better: Use NodeType schema to determine scope name? 
                                // Or define Scope in StructureNodeDefinition?
                                // Fallback: Use NodeTypeId as Scope? (e.g. StageFolder -> Scope Stage)
                                // Let's use NodeTypeId or defined Scope. Propose Scope property later.
                                // For now, allow {Stage.Code} implies key "Stage". 
                                // Let's guess scope from SourceKey (Stages -> Stage) or use a convention.
                                // Convention: NodeType.Key usually maps to domain concept.
                                // Let's put specific key defined in Schema? 
                                // Simpler: Put it as key "Item" and allow renaming?
                                
                                // Working assumption: dictionary name is "Stages", we want context "Stages" or "Stage".
                                // For tokenizer {Stage.Code}, we need key "Stage".
                                string scope = def.SourceKey.EndsWith("s") ? def.SourceKey[..^1] : def.SourceKey; 
                                
                                var valDict = new Dictionary<string, object>
                                {
                                    { "Code", itemDef.Code },
                                    { "Name", itemDef.Name }
                                };
                                list.Add(new Dictionary<string, object> { { scope, valDict } });
                            }
                        }
                    }
                }
                break;

            case MultiplicitySource.Table:
                // SourceKey = Table Name (e.g. "Buildings")
                // Project.TableData["Buildings"] = List<Dict<string, object>>
                if (!string.IsNullOrEmpty(def.SourceKey) && 
                    project.TableData.TryGetValue(def.SourceKey, out var rows))
                {
                    string scope = def.SourceKey.EndsWith("s") ? def.SourceKey[..^1] : def.SourceKey;
                    
                    foreach (var row in rows)
                    {
                        list.Add(new Dictionary<string, object> { { scope, row } });
                    }
                }
                break;
        }

        return list;
    }

    private string RemovePlural(string s)
    {
        if (s.EndsWith("es")) return s[..^2]; // Box -> Boxes ? No, Classes -> Class
        if (s.EndsWith("s")) return s[..^1];
        return s;
    }

    private bool EvaluateCondition(string? expression, Dictionary<string, object> context, Project project)
    {
        if (string.IsNullOrWhiteSpace(expression)) return true;
        // Parse simple expressions: "Stage.Code == P"
        // TODO: Implement expression parser
        return true; 
    }
}
