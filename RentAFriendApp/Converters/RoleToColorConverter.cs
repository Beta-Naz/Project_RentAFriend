using System.Windows.Media;

namespace RentAFriendApp.Converters
{
    public class RoleToColorConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string role)
            {
                switch (role)
                {
                    case "Admin": new SolidColorBrush(Color.FromRgb(156, 39, 176)); break; // Фиолетовый
                    case "Moderator": new SolidColorBrush(Color.FromRgb(255, 152, 0)); break; // Оранжевый
                    case "Friend": new SolidColorBrush(Color.FromRgb(76, 175, 80)); break; // Зеленый
                    case "Client": new SolidColorBrush(Color.FromRgb(33, 150, 243)); break; // Синий
                    default: new SolidColorBrush(Color.FromRgb(158, 158, 158)); break; // Серый
                }
                ;
            }
            return new SolidColorBrush(Color.FromRgb(158, 158, 158));
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
