using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Input;

namespace RentAFriendApp.Converters
{
    public class BoolToCursorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isAvailable && isAvailable)
            {
                return Cursors.Hand;
            }
            return Cursors.Arrow;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}