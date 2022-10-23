using System.Globalization;
using System.Windows.Data;

namespace CopyDatabase.Converters;

internal sealed class SecureToPasswordConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
        if (value is not SecureString secure) return string.Empty;
        return secure.FromSecureString();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
        if (value is not string v) return new SecureString();
        return v.ToSecureString();
    }
}
