using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AGK.ProjectGen.UI.Converters;

public class BoolToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool boolValue = value is bool b && b;
        if (Invert)
            return boolValue ? Visibility.Collapsed : Visibility.Visible;
        
        return boolValue ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Visibility visibility)
            return Invert ? visibility != Visibility.Visible : visibility == Visibility.Visible;
        
        return false;
    }
}
