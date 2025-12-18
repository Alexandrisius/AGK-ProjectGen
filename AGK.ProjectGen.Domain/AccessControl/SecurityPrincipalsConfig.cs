namespace AGK.ProjectGen.Domain.AccessControl;

public class SecurityPrincipalsConfig
{
    public List<SecurityPrincipal> Groups { get; set; } = new();
    public List<SecurityPrincipal> Users { get; set; } = new();
}
