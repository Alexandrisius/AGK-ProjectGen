namespace AGK.ProjectGen.Domain.Enums;

public enum AclApplyMode
{
    Additive,   // Добавить права, не удалять существующие
    Normalize,  // Привести в точное соответствие (удалить лишние)
    DoNotTouch  // Не менять права
}
