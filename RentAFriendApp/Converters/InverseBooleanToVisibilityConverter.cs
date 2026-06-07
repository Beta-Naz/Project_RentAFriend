using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RentAFriendApp.Converters
{
    public class InverseBooleanToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Значение для True (по умолчанию Collapsed)
        /// </summary>
        public Visibility TrueValue { get; set; } = Visibility.Collapsed;

        /// <summary>
        /// Значение для False (по умолчанию Visible)
        /// </summary>
        public Visibility FalseValue { get; set; } = Visibility.Visible;

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? TrueValue : FalseValue;
            }

            // Если значение null или не bool, возвращаем FalseValue по умолчанию
            return FalseValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                return visibility != TrueValue;
            }

            return false;
        }
    }
}
