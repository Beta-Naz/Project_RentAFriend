using System.Windows.Data;

namespace RentAFriendApp.Converters
{
    public class TimeSpanToDurationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is TimeSpan timeSpan)
            {
                if (timeSpan.TotalHours >= 1)
                {
                    int hours = (int)timeSpan.TotalHours;
                    int minutes = timeSpan.Minutes;

                    if (minutes > 0)
                        return $"{hours} ч {minutes} мин";
                    else
                        return $"{hours} час";
                }
                else
                {
                    return $"{timeSpan.TotalMinutes} мин";
                }
            }
            return "0 мин";
        }
         
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
