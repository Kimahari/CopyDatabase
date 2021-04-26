using System;
using System.Globalization;
using System.Security;
using System.Windows.Data;

using DataBaseCompare.Tools;

namespace DataBaseCompare.Converters {
    public class SecureStringValueConverter : IValueConverter {

        #region Public Methods

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return (value as SecureString).SecureStringToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return $"{value}".ToSecureString();
        }

        #endregion Public Methods
    }
}
