using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AGK.ProjectGen.UI.Converters;

/// <summary>
/// Конвертирует Count > 0 в Visibility.Visible, иначе Collapsed.
/// </summary>
public class CountToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int count)
            return count > 0 ? Visibility.Visible : Visibility.Collapsed;
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
