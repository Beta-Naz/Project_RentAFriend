using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
namespace RentAFriendApp.Converters
{
    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                switch (status)
                {
                    case "Pending": return new SolidColorBrush(Color.FromRgb(255, 152, 0));
                    case "Confirmed": return new SolidColorBrush(Color.FromRgb(76, 175, 80));
                    case "Completed": return new SolidColorBrush(Color.FromRgb(33, 150, 243));
                    case "Cancelled": return new SolidColorBrush(Color.FromRgb(244, 67, 54));
                    case "Rejected": return new SolidColorBrush(Color.FromRgb(158, 158, 158));
                    default: return new SolidColorBrush(Color.FromRgb(158, 158, 158));
                }
                ;
            }
            return new SolidColorBrush(Color.FromRgb(158, 158, 158));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}