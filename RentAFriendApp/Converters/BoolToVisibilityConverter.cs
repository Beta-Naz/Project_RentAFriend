using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RentAFriendApp.Converters
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool boolValue = value is bool b && b;
            bool inverse = parameter is string s && s == "Inverse";
            return inverse ? (boolValue ? Visibility.Collapsed : Visibility.Visible)
                           : (boolValue ? Visibility.Visible : Visibility.Collapsed);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
