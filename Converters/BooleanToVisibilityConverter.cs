using DataBaseCompare.Tools;
using System;
using System.Globalization;
using System.Security;
using System.Windows;
using System.Windows.Data;

namespace DataBaseCompare.Converters {

    /// <summary>
    /// Converts Boolean Values to Control.Visibility values
    /// </summary>
    public class BooleanToVisibilityConverter : IValueConverter {

        #region Fields

        //Set to true if you just want to hide the control
        //else set to false if you want to collapse the control
        private bool isHidden;

        //Set to true if you want to show control when boolean value is true
        //Set to false if you want to hide/collapse control when value is true
        private bool triggerValue;

        #endregion Fields

        #region Properties

        public bool IsHidden {
            get => isHidden;
            set => isHidden = value;
        }

        public bool TriggerValue {
            get => triggerValue;
            set => triggerValue = value;
        }

        #endregion Properties

        #region Methods

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return GetVisibility(value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }

        private object GetVisibility(object value) {
            if (!(value is bool))
                return DependencyProperty.UnsetValue;
            var objValue = (bool) value;
            if ((objValue && TriggerValue && IsHidden) || (!objValue && !TriggerValue && IsHidden)) {
                return Visibility.Hidden;
            }
            if ((objValue && TriggerValue && !IsHidden) || (!objValue && !TriggerValue && !IsHidden)) {
                return Visibility.Collapsed;
            }
            return Visibility.Visible;
        }

        #endregion Methods
    }

    public class HasValueConverter : IValueConverter {

        #region Methods

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return !String.IsNullOrEmpty($"{value}") ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }

        #endregion Methods
    }

    public class SecureStringValueConverter : IValueConverter {

        #region Methods

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return (value as SecureString).SecureStringToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return $"{value}".ToSecureString();
        }

        #endregion Methods
    }
}
