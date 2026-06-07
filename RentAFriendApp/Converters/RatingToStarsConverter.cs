using System.Globalization;
using System.Windows.Data;

namespace RentAFriendApp.Converters
{
    public class RatingToStarsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int rating && rating >= 1 && rating <= 5)
            {
                var stars = new List<int>();
                for (int i = 0; i < rating; i++)
                {
                    stars.Add(1);
                }
                return stars;
            }
            return new List<int>();
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class RatingToStarsConverterFigure : IValueConverter //Тот же самый RatingToStarsConverter, но со звездочками
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double rating)
            {
                int fullStars = (int)rating;
                bool hasHalfStar = rating - fullStars >= 0.5;

                string stars = new string('★', fullStars);
                if (hasHalfStar) stars += "½";
                stars += new string('☆', 5 - fullStars - (hasHalfStar ? 1 : 0));

                return stars;
            }
            return "☆☆☆☆☆";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
