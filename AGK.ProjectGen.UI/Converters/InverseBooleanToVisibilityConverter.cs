using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AGK.ProjectGen.UI.Converters;

public class InverseBooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b && b) return Visibility.Collapsed;
        if (value != null) return Visibility.Collapsed; // If not null (object instance), collapse? 
        // Logic: if value is True or Not Null (if object), then Checked.
        // Wait, binding is SelectedProfile (object).
        // If SelectedProfile != null -> Visibility.Visible (for Editor)
        // If SelectedProfile == null -> Visibility.Collapsed (for Editor)
        // 
        // This converter is "InverseBooleanToVisibility". 
        // Usage: Visibility="{Binding SelectedProfile, Converter=Inverse...}"
        // If SelectedProfile is present, this should return Collapsed (to hide "Select profile..." text).
        // If SelectedProfile is null, this should return Visible.
        
        // Standard BooleanToVisibility treats: true -> Visible, false -> Collapsed.
        // It doesn't automatically convert Object to bool (ProjectSchema -> true).
        // I need ObjectToVisibilityConverter really.
        
        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
