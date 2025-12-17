using System;
using System.Globalization;
using System.Windows.Data;

namespace AGK.ProjectGen.UI.Converters;

/// <summary>
/// Конвертер для двустороннего биндинга enum значений.
/// Принимает строковое представление enum и конвертирует в/из enum.
/// </summary>
public class EnumToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null)
            return string.Empty;
        
        return value.ToString()!;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string strValue && targetType.IsEnum)
        {
            if (Enum.TryParse(targetType, strValue, out var result))
                return result;
        }
        
        return Binding.DoNothing;
    }
}
