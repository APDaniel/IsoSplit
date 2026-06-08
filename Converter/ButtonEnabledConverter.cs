using System;
using System.Globalization;
using System.Windows.Data;

namespace IsoSplitProject.Converter
{
    public class ButtonEnabledConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // Check the conditions and return the appropriate boolean value
            if (values.Length >= 3 && values[0] != null && values[1] is int marginValue && values[2] is int margin2Value)
            {
                return marginValue >= -50 && marginValue <= 50 && margin2Value >= -50 && margin2Value <= 50;
            }
            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
