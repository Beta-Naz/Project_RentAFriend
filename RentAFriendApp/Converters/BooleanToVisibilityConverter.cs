using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RentAFriendApp.Converters
{
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                // Если передан параметр "Inverse", инвертируем логику
                if (parameter is string param && param.Equals("Inverse", StringComparison.OrdinalIgnoreCase))
                {
                    return boolValue ? Visibility.Collapsed : Visibility.Visible;
                }
                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                // Если передан параметр "Inverse", инвертируем логику
                if (parameter is string param && param.Equals("Inverse", StringComparison.OrdinalIgnoreCase))
                {
                    return visibility != Visibility.Visible;
                }
                return visibility == Visibility.Visible;
            }
            return false;
        }
    }
}
