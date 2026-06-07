using System.Windows.Data;

namespace RentAFriendApp.Converters
{
    public class StatusToTextConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var status = value as string;
            return status switch
            {
                "Pending" => "Ожидает",
                "Confirmed" => "Подтверждено",
                "Completed" => "Завершено",
                "Cancelled" => "Отменено",
                "Rejected" => "Отклонено",
                _ => status,
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
