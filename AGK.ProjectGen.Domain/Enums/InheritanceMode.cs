namespace AGK.ProjectGen.Domain.Enums;

public enum InheritanceMode
{
    Keep,           // Оставить как есть (наследовать)
    BreakCopy,      // Разорвать и скопировать
    BreakClear      // Разорвать и очистить
}
