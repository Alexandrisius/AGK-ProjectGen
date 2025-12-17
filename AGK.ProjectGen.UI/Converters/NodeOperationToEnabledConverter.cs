using System;
using System.Globalization;
using System.Windows.Data;
using AGK.ProjectGen.Domain.Entities;

namespace AGK.ProjectGen.UI.Converters;

/// <summary>
/// Конвертер для блокировки чекбокса при Operation = Delete (нельзя включить удаляемую папку).
/// </summary>
public class NodeOperationToEnabledConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is NodeOperation operation)
        {
            // Чекбокс отключён для удаляемых папок
            return operation != NodeOperation.Delete;
        }
        return true;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
