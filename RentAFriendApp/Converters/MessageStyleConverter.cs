using System.Windows;
using System.Windows.Data;

namespace RentAFriendApp.Converters
{
    public class MessageStyleConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool isOutgoing)
            {
                var styleKey = isOutgoing ? "OutgoingMessageStyle" : "IncomingMessageStyle";
                return Application.Current.MainWindow?.FindResource(styleKey) as Style;
            }
            return Application.Current.MainWindow?.FindResource("IncomingMessageStyle") as Style;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}