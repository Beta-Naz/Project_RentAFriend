using System.Windows.Media;

namespace RentAFriendApp.Converters
{
    public class ValidationToColorConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string validation)
            {
                if (validation.StartsWith('✓'))
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
                else if (validation.StartsWith('⚠'))
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF9800"));
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
