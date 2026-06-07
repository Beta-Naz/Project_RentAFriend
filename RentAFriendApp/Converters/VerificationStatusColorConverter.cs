using System.Windows.Media;

namespace RentAFriendApp.Converters
{
    public class VerificationStatusColorConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool isVerified)
            {
                return isVerified ?
                    new SolidColorBrush(Color.FromRgb(76, 175, 80)) : // Зеленый
                    new SolidColorBrush(Color.FromRgb(255, 152, 0));  // Оранжевый
            }
            return new SolidColorBrush(Color.FromRgb(158, 158, 158));
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
