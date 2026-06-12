using RentAFriendApp.Models.ClassesDTO.FriendProfileDTO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace RentAFriendApp.Views.Controls
{
    public partial class FriendCard : UserControl
    {
        private FPInfoDTO _friend;

        public event Action<FPInfoDTO>? CardClicked;
        public event Action<FPInfoDTO>? ViewRequested;

        public FriendCard()
        {
            InitializeComponent();
        }

        public void SetFriend(FPInfoDTO friend)
        {
            _friend = friend;

            NameText.Text = friend.FullName ?? "Без имени";
            CityText.Text = !string.IsNullOrEmpty(friend.City) ? $"📍 {friend.City}" : "";
            BioText.Text = !string.IsNullOrEmpty(friend.Bio)
                ? (friend.Bio.Length > 100 ? friend.Bio[..100] + "..." : friend.Bio)
                : "Нет описания";
            PriceText.Text = friend.HourlyRate.HasValue ? $"{friend.HourlyRate.Value:N0} ₽/час" : "Бесплатно";

            // Инициалы
            InitialsText.Text = GetInitials(friend.FullName);

            // Рейтинг
            if (friend.AverageRating.HasValue && friend.AverageRating > 0)
            {
                RatingText.Text = $"⭐ {friend.AverageRating.Value:F1}";
                RatingText.Visibility = Visibility.Visible;
            }
            else
            {
                RatingText.Visibility = Visibility.Collapsed;
            }

            // Хобби
            HobbiesPanel.Children.Clear();
            if (!string.IsNullOrEmpty(friend.Hobbies))
            {
                var hobbies = friend.Hobbies.Split(',').Select(h => h.Trim()).Take(3);
                foreach (var hobby in hobbies)
                {
                    HobbiesPanel.Children.Add(new Border
                    {
                        Background = new SolidColorBrush(Color.FromRgb(232, 245, 233)),
                        CornerRadius = new CornerRadius(8),
                        Padding = new Thickness(6, 2, 6, 2),
                        Margin = new Thickness(0, 0, 4, 4),
                        Child = new TextBlock
                        {
                            Text = hobby,
                            FontSize = 10,
                            Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50))
                        }
                    });
                }
            }
        }

        private static string GetInitials(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "?";
            var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length >= 2
                ? $"{parts[0][0]}{parts[^1][0]}".ToUpper()
                : name[..Math.Min(2, name.Length)].ToUpper();
        }

        private void Card_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_friend != null)
                ViewRequested?.Invoke(_friend);
        }

        private void Card_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            RootBorder.Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                BlurRadius = 20,
                Opacity = 0.15,
                ShadowDepth = 2,
                Color = Colors.Black
            };
        }

        private void Card_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            RootBorder.Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                BlurRadius = 12,
                Opacity = 0.06,
                ShadowDepth = 1
            };
        }
    }
}