namespace AGK.ProjectGen.Domain.Enums;

public enum NodeType
{
    ProjectRoot,
    Stage,      // Стадия (П, Р, и т.д.)
    Building,   // Здание/Сооружение
    Discipline, // Раздел (АР, КР...)
    System,     // Служебная (Рабочая, Архив...)
    Custom      // Пользовательская
}
