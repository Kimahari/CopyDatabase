using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DataBaseCompare.Converters {
    public class HasValueConverter : IValueConverter {

        #region Public Methods

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return !string.IsNullOrEmpty($"{value}") ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }

        #endregion Public Methods
    }
}
