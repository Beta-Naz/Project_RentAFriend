using System.Globalization;
using System.Windows.Data;

namespace RentAFriendApp.Converters
{
    public class DateToDayOfWeekConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime date)
            {
                string[] daysOfWeek = { "Пн", "Вт", "Ср", "Чт", "Пт", "Сб", "Вс" };
                int dayIndex = (int)date.DayOfWeek;
                if (dayIndex == 0) dayIndex = 6; 
                else dayIndex--;

                return daysOfWeek[dayIndex];
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
