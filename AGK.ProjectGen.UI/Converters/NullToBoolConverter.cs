using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AGK.ProjectGen.UI.Converters;

/// <summary>
/// Конвертер для преобразования null/empty-string в bool.
/// Возвращает true если значение null или пустая строка.
/// </summary>
public class NullToBoolConverter : IValueConverter
{
    public bool Invert { get; set; } = false;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool isNullOrEmpty = value == null || (value is string s && string.IsNullOrEmpty(s));
        return Invert ? !isNullOrEmpty : isNullOrEmpty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
