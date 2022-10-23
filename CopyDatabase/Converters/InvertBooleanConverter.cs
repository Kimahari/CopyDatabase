﻿using System.Globalization;
using System.Windows.Data;

namespace CopyDatabase.Converters;

internal class InvertBooleanConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
        if (value is not bool b) return true;
        return !b;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
        throw new NotImplementedException();
    }
}