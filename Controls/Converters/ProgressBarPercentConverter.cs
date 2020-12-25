using System;
using System.Globalization;
using System.Windows.Data;

namespace Controls.Converters
{
    internal class ProgressBarPercentConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length != 2) return "";
            if (!double.TryParse(values[0].ToString(), out double value)) return "";
            if (!double.TryParse(values[1].ToString(), out double maximum)) return "";
            return value / maximum;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
