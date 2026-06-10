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
            return "09:00";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str && !string.IsNullOrWhiteSpace(str))
            {
                if (TimeSpan.TryParseExact(str, new[] { @"hh\:mm", @"h\:mm", @"hh\:m", @"h\:m" },
                    CultureInfo.InvariantCulture, out TimeSpan result))
                {
                    return result;
                }
            }
            return Binding.DoNothing;
        }
    }
}