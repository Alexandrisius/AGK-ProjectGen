using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AGK.ProjectGen.UI.Converters;

public class NullToVisibilityConverter : IValueConverter
{
    // If True, Null = Visible, NotNull = Collapsed (Inverse)
    public bool Invert { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool isNull = value == null;
        if (Invert)
            return isNull ? Visibility.Visible : Visibility.Collapsed;
        
        return isNull ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
