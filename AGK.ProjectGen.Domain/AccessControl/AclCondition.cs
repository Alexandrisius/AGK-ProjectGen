using System.Text.Json.Serialization;
using AGK.ProjectGen.Domain.Enums;

namespace AGK.ProjectGen.Domain.AccessControl;

public class AclCondition
{
    /// <summary>
    /// Путь к атрибуту из контекста узла.
    /// Примеры: "Discipline.Code", "Stage.Name", "SystemFolder.Name"
    /// Контекст наследуется от родительских узлов при генерации дерева.
    /// </summary>
    public string AttributePath { get; set; } = string.Empty;
    
    /// <summary>
    /// Оператор сравнения.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ConditionOperator Operator { get; set; } = ConditionOperator.Equals;
    
    /// <summary>
    /// Значение для сравнения.
    /// </summary>
    public string Value { get; set; } = string.Empty;
    
    /// <summary>
    /// Возвращает строковое представление условия для отображения.
    /// </summary>
    public override string ToString()
    {
        var op = Operator switch
        {
            ConditionOperator.Equals => "==",
            ConditionOperator.NotEquals => "!=",
            ConditionOperator.Contains => "Contains",
            ConditionOperator.NotContains => "NotContains",
            ConditionOperator.GreaterThan => ">",
            ConditionOperator.LessThan => "<",
            ConditionOperator.GreaterThanOrEqual => ">=",
            ConditionOperator.LessThanOrEqual => "<=",
            _ => "=="
        };
        return $"{AttributePath} {op} '{Value}'";
    }
}
