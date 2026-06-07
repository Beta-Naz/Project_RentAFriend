using System.Globalization;
using System.Windows.Data;

namespace RentAFriendApp.Converters
{
    public class DateTimeToChatTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime dateTime)
            {
                if (dateTime.Date == DateTime.Today)
                    return dateTime.ToString("HH:mm");
                else if (dateTime.Date == DateTime.Today.AddDays(-1))
                    return "Вчера";
                else
                    return dateTime.ToString("dd.MM");
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
