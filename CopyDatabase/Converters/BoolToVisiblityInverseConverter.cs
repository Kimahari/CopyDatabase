using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CopyDatabase.Converters;

internal sealed class BoolToVisiblityInverseConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
        if (value is not bool b || b) return Visibility.Collapsed;
        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
        if (value is not Visibility v) return false;
        return v == Visibility.Collapsed;
    }
}
