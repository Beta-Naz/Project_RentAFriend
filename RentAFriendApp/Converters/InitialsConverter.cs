using System.Globalization;
using System.Windows.Data;

namespace RentAFriendApp.Converters
{
    public class InitialsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string fullName)
            {
                if (string.IsNullOrWhiteSpace(fullName))
                    return "??";

                var parts = fullName.Split(' ', (char)StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    return $"{parts[0][0]}{parts[1][0]}".ToUpper();
                }
                else if (parts.Length == 1)
                {
                    return parts[0].Length >= 2 ? parts[0][..2].ToUpper() : parts[0].ToUpper();
                }
            }
            return "??";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
