using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DftMosaic.Desktop.Xaml.Converters
{
    internal class RectToIntegerStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is Rect m ? $"{(int)m.X},{(int)m.Y},{(int)m.Width},{(int)m.Height}" : Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
