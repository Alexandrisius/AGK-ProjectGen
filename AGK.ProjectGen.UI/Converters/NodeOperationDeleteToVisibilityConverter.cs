using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using AGK.ProjectGen.Domain.Entities;

namespace AGK.ProjectGen.UI.Converters;

/// <summary>
/// Конвертер для отображения DELETE метки при Operation = Delete.
/// </summary>
public class NodeOperationDeleteToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is NodeOperation operation)
        {
            if (operation == NodeOperation.Delete)
            {
                return Visibility.Visible;
            }
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
