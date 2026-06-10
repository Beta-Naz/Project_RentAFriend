using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace RentAFriendApp.Converters.Admin
{
    public class InitialsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string name && !string.IsNullOrWhiteSpace(name))
            {
                var parts = name.Split(' ');
                return parts.Length >= 2
                    ? $"{parts[0][0]}{parts[1][0]}".ToUpper()
                    : name[..Math.Min(2, name.Length)].ToUpper();
            }
            return "??";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class RoleToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var color = (value as string) switch
            {
                "Admin" => Color.FromRgb(156, 39, 176),
                "Moderator" => Color.FromRgb(255, 152, 0),
                "Friend" => Color.FromRgb(76, 175, 80),
                "Client" => Color.FromRgb(33, 150, 243),
                _ => Color.FromRgb(158, 158, 158)
            };
            return new SolidColorBrush(color);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isActive = value is true;
            return new SolidColorBrush(isActive
                ? Color.FromRgb(76, 175, 80)
                : Color.FromRgb(244, 67, 54));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool boolValue = value is true;
            if (parameter?.ToString() == "reverse") boolValue = !boolValue;
            return boolValue ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class BooleanToStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is true ? "Активен" : "Заблокирован";

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class TrimTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string text)
            {
                int max = parameter is string p && int.TryParse(p, out int m) ? m : 100;
                return text.Length > max ? text[..max] + "..." : text;
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class VerificationStatusColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isVerified = value is true;
            return new SolidColorBrush(isVerified
                ? Color.FromRgb(76, 175, 80)
                : Color.FromRgb(255, 152, 0));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class VerificationStatusTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is true ? "Проверен" : "На проверке";

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class ActionTypeColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string action)
            {
                Color color = action.Contains("DELETE") || action.Contains("BLOCK")
                    ? Color.FromRgb(244, 67, 54)
                    : action.Contains("CREATE") || action.Contains("VERIFY")
                        ? Color.FromRgb(76, 175, 80)
                        : action.Contains("UPDATE")
                            ? Color.FromRgb(255, 152, 0)
                            : Color.FromRgb(158, 158, 158);
                return new SolidColorBrush(color);
            }
            return new SolidColorBrush(Color.FromRgb(158, 158, 158));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class ReadStatusColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isRead = value is true;
            return new SolidColorBrush(isRead
                ? Color.FromRgb(76, 175, 80)
                : Color.FromRgb(255, 152, 0));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class ReadStatusTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is true ? "✓" : "!";

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class BookingStatusColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                Color color = status switch
                {
                    "Confirmed" => Color.FromRgb(76, 175, 80),
                    "Pending" => Color.FromRgb(255, 152, 0),
                    "Completed" => Color.FromRgb(33, 150, 243),
                    "Cancelled" => Color.FromRgb(244, 67, 54),
                    "Rejected" => Color.FromRgb(244, 67, 54),
                    _ => Color.FromRgb(158, 158, 158)
                };
                return new SolidColorBrush(color);
            }
            return new SolidColorBrush(Color.FromRgb(158, 158, 158));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
