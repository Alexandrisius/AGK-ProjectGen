using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;
using AGK.ProjectGen.Application.Interfaces;
using AGK.ProjectGen.Domain.AccessControl;
using AGK.ProjectGen.Domain.Enums;

namespace AGK.ProjectGen.Infrastructure.Services;

[SupportedOSPlatform("windows")]
public class AclService : IAclService
{
    public void SetDirectoryAcl(string path, List<AclRule> rules, InheritanceMode inheritanceMode, AclApplyMode applyMode)
    {
        if (applyMode == AclApplyMode.DoNotTouch) return;
        if (!Directory.Exists(path)) return;

        var dirInfo = new DirectoryInfo(path);
        var searchConfig = new DirectorySecurity(path, AccessControlSections.Access); // Read current

        // 1. Inheritance
        bool protectionParams = inheritanceMode != InheritanceMode.Keep;
        bool preserveInherited = inheritanceMode == InheritanceMode.BreakCopy; // true if copy, false if clear (only valid if protected)
        
        // If keeping inheritance, protection is false. If breaking, protection is true.
        // SetAccessRuleProtection(isProtected, preserveInheritance)
        searchConfig.SetAccessRuleProtection(protectionParams, preserveInherited);

        // 2. Logic for Access Rules
        // If Normalize, we explicitly PURGE explicit rules that are not in the list?
        // Or essentially we rely on AddAccessRule vs ResetAccessRule?
        // DirectorySecurity doesn't have "ClearAll" easily without clearing inherited too, 
        // but since we handled inheritance protection above, we can consider removing explicit rules.

        if (applyMode == AclApplyMode.Normalize)
        {
            // Remove all explicit audit/access rules? 
            // Just Access rules.
            // Get all rules
            var currentRules = searchConfig.GetAccessRules(true, false, typeof(NTAccount)); 
            // true=includeExplicit, false=includeInherited (we only want to clear explicit)
            
            foreach (FileSystemAccessRule rule in currentRules)
            {
                searchConfig.RemoveAccessRule(rule);
            }
        }

        // 3. Всегда добавляем полные права для текущего пользователя (создателя)
        // ВАЖНО: это делается ПОСЛЕ очистки explicit rules, чтобы правило не удалялось
        var currentUser = WindowsIdentity.GetCurrent();
        if (currentUser.User != null)
        {
            var creatorRule = new FileSystemAccessRule(
                currentUser.User,
                FileSystemRights.FullControl,
                InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                PropagationFlags.None,
                AccessControlType.Allow
            );
            searchConfig.AddAccessRule(creatorRule);
        }

        // 4. Add Rules from profile
        foreach (var rule in rules)
        {
            try
            {
                var identity = new NTAccount(rule.Identity);
                var rights = MapRights(rule.Rights);
                var type = rule.IsDeny ? AccessControlType.Deny : AccessControlType.Allow;
                
                // Typical simplified rule for containers: ContainerInherit + ObjectInherit
                var fsRule = new FileSystemAccessRule(
                    identity, 
                    rights, 
                    InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, 
                    PropagationFlags.None, 
                    type);

                searchConfig.AddAccessRule(fsRule);
            }
            catch (Exception ex)
            {
                // Fallback: log or ignore? Throwing is better for GIP tool.
                throw new InvalidOperationException($"Failed to apply ACL rule for {rule.Identity}: {ex.Message}", ex);
            }
        }

        // 5. Commit to disk
        dirInfo.SetAccessControl(searchConfig);
    }

    public List<AclRule> GetDirectoryAcl(string path)
    {
        var result = new List<AclRule>();
        if (!Directory.Exists(path)) return result;

        var dirInfo = new DirectoryInfo(path);
        var security = dirInfo.GetAccessControl();
        
        var rules = security.GetAccessRules(true, true, typeof(NTAccount));
        foreach (FileSystemAccessRule rule in rules)
        {
            // Reverse mapping (approximate)
            result.Add(new AclRule
            {
                Identity = rule.IdentityReference.Value,
                Rights = MapFsRights(rule.FileSystemRights),
                IsDeny = rule.AccessControlType == AccessControlType.Deny
            });
        }
        return result;
    }

    private FileSystemRights MapRights(ProjectAccessRights rights)
    {
        FileSystemRights fsRights = 0;
        if (rights.HasFlag(ProjectAccessRights.Read)) fsRights |= FileSystemRights.ReadAndExecute; // Standard read
        if (rights.HasFlag(ProjectAccessRights.Write)) fsRights |= FileSystemRights.Write | FileSystemRights.Modify; // Write often implies Modify in simple terms, or just Write?
        // Let's be specific
        if (rights == ProjectAccessRights.FullControl) return FileSystemRights.FullControl;
        
        if (rights.HasFlag(ProjectAccessRights.Modify)) fsRights |= FileSystemRights.Modify;
        
        // Exact mapping logic depends on user need.
        // T Z says: Read/Write/Modify/Full.
        // Read usually means Read & Execute + List Folder + Read Data.
        
        if (fsRights == 0) fsRights = FileSystemRights.ReadAndExecute; // Default safely?
        
        return fsRights;
    }

    private ProjectAccessRights MapFsRights(FileSystemRights fsRights)
    {
        // Approximate for display
        if (fsRights.HasFlag(FileSystemRights.FullControl)) return ProjectAccessRights.FullControl;
        if (fsRights.HasFlag(FileSystemRights.Modify)) return ProjectAccessRights.Modify;
        if (fsRights.HasFlag(FileSystemRights.Write)) return ProjectAccessRights.Write;
        return ProjectAccessRights.Read;
    }
}
