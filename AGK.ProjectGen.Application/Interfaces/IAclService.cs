using AGK.ProjectGen.Domain.AccessControl;
using AGK.ProjectGen.Domain.Enums;

namespace AGK.ProjectGen.Application.Interfaces;

public interface IAclService
{
    void SetDirectoryAcl(string path, List<AclRule> rules, InheritanceMode inheritanceMode, AclApplyMode applyMode);
    
    // For verification or diff
    List<AclRule> GetDirectoryAcl(string path);
}
