namespace AGK.ProjectGen.Domain.Enums;

public enum AttributeType
{
    String,
    Integer,
    Boolean,
    Date,
    Select,     // Выбор из списка
    MultiSelect,// Множественный выбор
    Table       // Таблица объектов (например список зданий)
}
