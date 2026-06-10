using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace RentAFriendApp.Converters
{
    public class SlotTextColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isAvailable && isAvailable)
            {
                return new SolidColorBrush(Color.FromRgb(46, 125, 50)); // #2E7D32 - темно-зеленый
            }
            return new SolidColorBrush(Color.FromRgb(153, 153, 153)); // #999999 - серый
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}