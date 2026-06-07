using System.Windows.Media;

namespace RentAFriendApp.Converters
{
    public class ActionTypeColorConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string action)
            {
                if (action.Contains("DELETE") || action.Contains("BLOCK"))
                    return new SolidColorBrush(Color.FromRgb(244, 67, 54)); // Красный
                else if (action.Contains("CREATE") || action.Contains("VERIFY"))
                    return new SolidColorBrush(Color.FromRgb(76, 175, 80)); // Зеленый
                else if (action.Contains("UPDATE"))
                    return new SolidColorBrush(Color.FromRgb(255, 152, 0)); // Оранжевый
            }
            return new SolidColorBrush(Color.FromRgb(158, 158, 158)); // Серый
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
