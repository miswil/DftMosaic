using System;
using System.Globalization;
using System.Windows.Data;

namespace DftMosaic.Desktop.Xaml.Converters
{
    internal class BooleanToEnumConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Enum.Equals(value, parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool tf && tf)
            {
                return parameter;
            }
            return Binding.DoNothing;
        }
    }
}
