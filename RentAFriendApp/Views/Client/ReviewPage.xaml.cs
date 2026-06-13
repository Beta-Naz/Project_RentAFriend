using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using RentAFriendApp.ViewModels.Base;
using RentAFriendApp.ViewModels.Client;

namespace RentAFriendApp.Views.Client
{
    public partial class ReviewPage : Page
    {
        private readonly ReviewViewModel _viewModel;
        private readonly List<Button> _starButtons = new();
        private int _selectedRating;

        public ReviewPage(string token, int bookingId)
        {
            InitializeComponent();
            _viewModel = new ReviewViewModel(token, bookingId);
            DataContext = _viewModel;

            _starButtons.AddRange([Star1, Star2, Star3, Star4, Star5]);
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.3));
            BeginAnimation(OpacityProperty, fadeIn);
        }

        private void Star_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button clickedStar) return;

            int index = _starButtons.IndexOf(clickedStar);
            if (index < 0) return;

            _selectedRating = index + 1;

            for (int i = 0; i < _starButtons.Count; i++)
            {
                var template = _starButtons[i].Template;
                var starText = template?.FindName("star", _starButtons[i]) as TextBlock;
                if (starText != null)
                {
                    starText.Foreground = i < _selectedRating
                        ? new SolidColorBrush(Color.FromRgb(255, 193, 7))
                        : new SolidColorBrush(Color.FromRgb(224, 224, 224));
                }
            }

            RatingLabel.Text = _selectedRating switch
            {
                1 => "(0_0) Очень плохо",
                2 => "(-0-) Плохо",
                3 => "(-_-) Нормально",
                4 => "(0v0) Хорошо",
                5 => "(0^0) Отлично!",
                _ => "(-_-) Не знаю"
            };
            RatingLabel.Foreground = new SolidColorBrush(Color.FromRgb(76, 175, 80));

            _viewModel.Rating = _selectedRating;
            UpdateSubmitButton();
        }

        private void TitleTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _viewModel.Title = TitleTextBox.Text;
            TitleCharCount.Text = TitleTextBox.Text.Length.ToString();
        }

        private void ReviewTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _viewModel.ReviewText = ReviewTextBox.Text;
            CharCount.Text = ReviewTextBox.Text.Length.ToString();
            UpdateSubmitButton();
        }

        private void UpdateSubmitButton()
        {
            BtnSubmit.IsEnabled = _selectedRating > 0 && ReviewTextBox.Text.Trim().Length >= 20;
        }

        private async void BtnSubmit_Click(object sender, RoutedEventArgs e)
        {
            BtnSubmit.IsEnabled = false;
            BtnSubmit.Content = "⏳ Отправка...";

            try
            {
                if (_viewModel.SubmitReviewCommand is RelayCommandAsync cmd)
                    await cmd.ExecuteAsync(null);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                BtnSubmit.IsEnabled = true;
                BtnSubmit.Content = "✅ Отправить отзыв";
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.GoBack();
        }
    }
}