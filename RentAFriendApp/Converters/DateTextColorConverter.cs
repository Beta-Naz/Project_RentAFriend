using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace RentAFriendApp.Converters
{
    public class DateTextColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isSelected && isSelected)
            {
                return new SolidColorBrush(Colors.White);
            }
            return new SolidColorBrush(Color.FromRgb(51, 51, 51)); // #333333
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}