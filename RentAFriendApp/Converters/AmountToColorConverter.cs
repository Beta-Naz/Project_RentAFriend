using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace RentAFriendApp.Converters
{
    public class AmountToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal amount)
            {
                if (amount > 5000) return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
                if (amount > 2000) return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2196F3"));
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF9800"));
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
