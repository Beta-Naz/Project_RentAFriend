namespace RentAFriendApp.Converters
{
    public class TrimTextConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string text)
            {
                int maxLength = 100;
                if (parameter is string param && int.TryParse(param, out int length))
                {
                    maxLength = length;
                }

                if (text.Length > maxLength)
                {
                    return string.Concat(text.AsSpan(0, maxLength), "...");
                }
                return text;
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
