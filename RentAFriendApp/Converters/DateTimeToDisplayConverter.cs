using System.Globalization;
using System.Windows.Data;

namespace RentAFriendApp.Converters
{
    public class DateTimeToDisplayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime dateTime)
            {
                var cultureInfo = new CultureInfo("ru-RU");
                if (dateTime.Date == DateTime.Today)
                    return $"Сегодня, {dateTime.ToString("d MMMM", cultureInfo)}";
                if (dateTime.Date == DateTime.Today.AddDays(1))
                    return $"Завтра, {dateTime.ToString("d MMMM", cultureInfo)}";
                if (dateTime.Date == DateTime.Today.AddDays(-1))
                    return $"Вчера, {dateTime.ToString("d MMMM", cultureInfo)}";

                return dateTime.ToString("dddd, d MMMM", cultureInfo);
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
