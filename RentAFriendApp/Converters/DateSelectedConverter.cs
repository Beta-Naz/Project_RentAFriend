using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace RentAFriendApp.Converters
{
    public class DateSelectedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isSelected && isSelected)
            {
                return new SolidColorBrush(Color.FromRgb(76, 175, 80)); // #4CAF50 - зеленый
            }
            return new SolidColorBrush(Color.FromRgb(248, 249, 250)); // #F8F9FA - светлый
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}