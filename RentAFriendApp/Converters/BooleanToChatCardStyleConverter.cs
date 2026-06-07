using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RentAFriendApp.Converters
{
    public class BooleanToChatCardStyleConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isSelected && isSelected)
            {
                // Возвращаем стиль для активной карточки
                if (Application.Current.TryFindResource("ActiveChatCardStyle") is Style activeStyle)
                {
                    return activeStyle;
                }
            }

            // Возвращаем обычный стиль
            if (Application.Current.TryFindResource("ChatCardStyle") is Style normalStyle)
            {
                return normalStyle;
            }

            return null;
        }

        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
