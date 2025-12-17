namespace AGK.ProjectGen.Domain.Enums;

[Flags]
public enum ProjectAccessRights
{
    None = 0,
    Read = 1,
    Write = 2,
    Modify = 4,
    FullControl = 8
}
