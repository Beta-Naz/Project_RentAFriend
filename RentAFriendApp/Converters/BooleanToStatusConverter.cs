namespace RentAFriendApp.Converters
{
    public class BooleanToStatusConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool isActive)
            {
                return isActive ? "Активен" : "Заблокирован";
            }
            return "Неизвестно";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
