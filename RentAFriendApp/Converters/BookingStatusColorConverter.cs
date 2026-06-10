using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace RentAFriendApp.Converters
{
   public class BookingStatusColorConverter : IValueConverter
{
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                return status switch
                {
                    "Confirmed" => new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                    "Pending" => new SolidColorBrush(Color.FromRgb(255, 152, 0)),
                    "Completed" => new SolidColorBrush(Color.FromRgb(33, 150, 243)),
                    "Cancelled" => new SolidColorBrush(Color.FromRgb(244, 67, 54)),
                    "Rejected" => new SolidColorBrush(Color.FromRgb(244, 67, 54)),
                    _ => new SolidColorBrush(Color.FromRgb(158, 158, 158))
                };
            }
            return new SolidColorBrush(Color.FromRgb(158, 158, 158));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
