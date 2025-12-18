using System.Globalization;
using System.Windows.Data;

namespace AGK.ProjectGen.UI.Converters;

/// <summary>
/// Конвертер для сравнения двух значений на равенство.
/// Используется для MultiBinding.
/// </summary>
public class EqualityConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values == null || values.Length < 2)
            return false;
        
        var first = values[0];
        var second = values[1];
        
        if (first == null && second == null)
            return true;
        
        if (first == null || second == null)
            return false;
        
        return first.Equals(second);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
