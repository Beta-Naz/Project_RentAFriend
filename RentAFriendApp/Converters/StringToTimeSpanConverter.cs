using System.Globalization;
using System.Windows.Data;

namespace RentAFriendApp.Converters
{
    public class StringToTimeSpanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TimeSpan timeSpan)
                return timeSpan.ToString(@"hh\:mm");
            return "00:00";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str)
            {
                try
                {
                    return TimeSpan.Parse(str);
                }
                catch
                {
                    return TimeSpan.Zero;
                }
            }
            return TimeSpan.Zero;
        }
    }
}
