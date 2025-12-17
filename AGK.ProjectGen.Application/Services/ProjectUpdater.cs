using AGK.ProjectGen.Application.Interfaces;
using AGK.ProjectGen.Domain.Entities;
using AGK.ProjectGen.Domain.Schema;
using AGK.ProjectGen.Domain.Enums;
using AGK.ProjectGen.Domain.AccessControl;

namespace AGK.ProjectGen.Application.Services;

public class ProjectUpdater : IProjectUpdater
{
    private readonly IFileSystemService _fileSystem;
    private readonly IAclService _aclService;

    public ProjectUpdater(IFileSystemService fileSystem, IAclService aclService)
    {
        _fileSystem = fileSystem;
        _aclService = aclService;
    }

    public List<GeneratedNode> CreateDiffPlan(GeneratedNode newStructure, string rootPath, ProfileSchema profile)
    {
        var plan = new List<GeneratedNode>();
        
        // Traverse the new structure and check against filesystem
        CheckNodeRecursive(newStructure, plan);

        // TODO: Scan for orphans (folders present on disk but not in structure)
        // This requires traversing the physical directory and comparing with newStructure paths.
        // For standard "Create" task, we mainly focus on what needs to be created.
        // For "Update", identifying removal candidates is key. 
        // We can add a separate pass or separate list for "Orphans".
        // GeneratedNode usually represents "Target". 
        // If we want to represent "Delete", we might need a separate list or attach "Phantom" nodes to the plan.
        
        return plan;
    }

    private void CheckNodeRecursive(GeneratedNode node, List<GeneratedNode> plan)
    {
        bool exists = _fileSystem.DirectoryExists(node.FullPath);
        node.Exists = exists;

        if (!exists)
        {
            node.Operation = NodeOperation.Create;
        }
        else
        {
            // If exists, checks ACL? 
            // For now, let's assume we might need to UpdateACLs depending on policy.
            // Or only if we explicitly requested "Enforce ACL".
            // Let's set UpdateAcl as potential operation if policy allows?
            // For MVP, we default to None if exists, unless forced.
            // But let's verify if we need to set Operation.
            
            // Should we check if it was a rename? 
            // Without ID persistence, we can't easily detect generic rename.
            
            node.Operation = NodeOperation.None; 
        }

        plan.Add(node);

        foreach (var child in node.Children)
        {
            CheckNodeRecursive(child, plan);
        }
    }

    public async Task ExecutePlanAsync(List<GeneratedNode> diffPlan, Project project, ProfileSchema profile)
    {
        // Sort by path length to ensure parents created first
        // Or rely on top-down traversal order (which diffPlan might not guarantee if flat list)
        // Safest: Sort by FullPath length or depth.
        
        var orderedPlan = diffPlan.OrderBy(n => n.FullPath.Length).ToList();

        foreach (var node in orderedPlan)
        {
            if (node.Operation == NodeOperation.Create)
            {
                _fileSystem.CreateDirectory(node.FullPath);
                // Apply ACL
                ApplyAcl(node, profile);
            }
            else if (node.Operation == NodeOperation.UpdateAcl)
            {
                ApplyAcl(node, profile); // Re-apply
            }
            // Deletes would go in reverse order (children first)
        }

        await Task.CompletedTask;
    }

    private void ApplyAcl(GeneratedNode node, ProfileSchema profile)
    {
        // 1. Find Rules for this Node
        // Based on Profile.AclBindings and NodeTypeId
        
        // Find bindings for this NodeTypeId
        var bindings = profile.AclBindings.Where(b => b.NodeTypeId == node.NodeTypeId).ToList();
        // Also could have specific Binding matching attributes (Conditions)
        
        var templatesToApply = new List<AclTemplate>();

        // Default templates from NodeType definition
        var nodeType = profile.NodeTypes.FirstOrDefault(nt => nt.TypeId == node.NodeTypeId);
        if (nodeType != null && nodeType.DefaultAclTemplates != null)
        {
            foreach (var tId in nodeType.DefaultAclTemplates)
            {
                var t = profile.AclTemplates.FirstOrDefault(at => at.Id == tId);
                if (t != null) templatesToApply.Add(t);
            }
        }

        // Templates from Bindings (Condition-based)
        foreach (var binding in bindings)
        {
            if (binding.Conditions.All(c => CheckCondition(c, node.ContextAttributes)))
            {
                var t = profile.AclTemplates.FirstOrDefault(at => at.Id == binding.TemplateId);
                if (t != null && !templatesToApply.Contains(t)) templatesToApply.Add(t);
            }
        }

        // Apply
        foreach (var template in templatesToApply)
        {
            // Mode? Usually Additive for multiple templates?
            // Or Last Wins?
            // T Z says: 1) Per-node 2) NodeType 3) Baseline
            // Actually AclService handles one "Set" call.
            // We should merge rules before calling SetDirectoryAcl?
            // AclTemplate has InheritanceMode. 
            // If multiple templates have conflicting inheritance? 
            // Use the "most specific" or the "Last" one.
            
            // Simplified: Apply each template sequentially.
            // CAUTION: BreakInheritance on 2nd template might clear 1st template rules if Mode=Ignore?
            // AclService SetDirectoryAcl handles one template logic usually.
            
            _aclService.SetDirectoryAcl(
                node.FullPath, 
                template.Rules, 
                template.Inheritance, 
                AclApplyMode.Additive // Default for composition
            ); 
        }
    }

    private bool CheckCondition(AclCondition condition, Dictionary<string, object> context)
    {
        // Basic check
        // AttributePath: "Stage.Code"
        // Context: Need to find "Stage" object, then "Code".
        // Or flat key "Stage.Code" mapping?
        // NamingEngine resolved tokens, we can reuse logic or simple dict lookup?
        // Let's assume context is nested: dict["Stage"] -> dict["Code"]
        
        // Simple parsing:
        var parts = condition.AttributePath.Split('.');
        if (parts.Length == 2)
        {
            if (context.TryGetValue(parts[0], out var obj))
            {
                if (obj is Dictionary<string, object> d && d.TryGetValue(parts[1], out var val))
                {
                    return val.ToString() == condition.Value;
                }
            }
        }
        return false;
    }
}
