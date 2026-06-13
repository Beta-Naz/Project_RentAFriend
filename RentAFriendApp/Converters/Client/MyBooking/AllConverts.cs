using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace RentAFriendApp.Converters.Client.MyBooking
{
    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.ToString() switch
            {
                "Pending" => new SolidColorBrush(Color.FromRgb(255, 152, 0)),
                "Confirmed" => new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                "Completed" => new SolidColorBrush(Color.FromRgb(33, 150, 243)),
                "Cancelled" => new SolidColorBrush(Color.FromRgb(244, 67, 54)),
                _ => new SolidColorBrush(Colors.Gray)
            };
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
    public class StatusToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.ToString() switch
            {
                "Pending" => "Ожидает",
                "Confirmed" => "Подтверждена",
                "Completed" => "Завершена",
                "Cancelled" => "Отменена",
                _ => value?.ToString() ?? ""
            };
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool boolValue = value is true;
            bool inverse = parameter?.ToString() == "Inverse";
            return inverse
                ? (boolValue ? Visibility.Collapsed : Visibility.Visible)
                : (boolValue ? Visibility.Visible : Visibility.Collapsed);
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    // ===== DATETIME → ДАТА (dd.MM) =====
    public class DateTimeToDateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime date)
                return date.ToString(parameter as string ?? "dd.MM");
            return "";
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
    public class TimeSpanToTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TimeSpan time)
                return time.ToString(@"hh\:mm");
            return "";
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
    public class PaymentStatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.ToString() switch
            {
                "Paid" => new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                "Unpaid" => new SolidColorBrush(Color.FromRgb(244, 67, 54)),
                _ => new SolidColorBrush(Colors.Gray)
            };
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
    public class PaymentStatusToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.ToString() switch
            {
                "Paid" => "Оплачено",
                "Unpaid" => "Не оплачено",
                _ => value?.ToString() ?? ""
            };
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    // ===== DURATION → ТЕКСТ (1 ч 30 мин) =====
    public class DurationToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TimeSpan duration)
            {
                if (duration.TotalHours >= 1)
                    return $"{(int)duration.TotalHours} ч {duration.Minutes} мин";
                return $"{duration.Minutes} мин";
            }
            return "";
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
