using RentAFriendApp.Context;
using RentAFriendApp.ViewModels.Client;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace RentAFriendApp.Views.Client
{
    public partial class PaymentPage : Page
    {
        private readonly PaymentViewModel _viewModel;

        public PaymentPage(string token, int bookingId, string friendName, DateTime date,
            TimeSpan startTime, TimeSpan endTime, decimal totalAmount)
        {
            InitializeComponent();
            _viewModel = new PaymentViewModel(token, bookingId, friendName, date, startTime, endTime, totalAmount);
            DataContext = _viewModel;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.25));
            BeginAnimation(OpacityProperty, fadeIn);
        }

        private void CardNumber_TextChanged(object sender, TextChangedEventArgs e)
        {
            var tb = sender as TextBox;
            if (tb == null) return;

            var text = tb.Text.Replace(" ", "");
            if (text.Length > 16) text = text[..16];

            if (text.Length >= 4)
            {
                var formatted = "";
                for (int i = 0; i < text.Length; i += 4)
                    formatted += text.Substring(i, Math.Min(4, text.Length - i)) + " ";
                tb.Text = formatted.Trim();
                tb.CaretIndex = tb.Text.Length;
            }

            _viewModel.CardNumber = text;
            UpdatePayButton();
        }

        private void Expiry_TextChanged(object sender, TextChangedEventArgs e)
        {
            var tb = sender as TextBox;
            if (tb == null) return;

            var text = tb.Text.Replace("/", "");
            if (text.Length > 4) text = text[..4];

            if (text.Length >= 2)
            {
                tb.Text = text[..2] + "/" + (text.Length > 2 ? text[2..] : "");
                tb.CaretIndex = tb.Text.Length;
            }

            _viewModel.Expiry = text;
            UpdatePayButton();
        }

        private void Cvv_TextChanged(object sender, TextChangedEventArgs e)
        {
            var tb = sender as TextBox;
            if (tb == null) return;

            var text = tb.Text;
            if (text.Length > 3) text = text[..3];
            tb.Text = text;
            tb.CaretIndex = tb.Text.Length;

            _viewModel.Cvv = text;
            UpdatePayButton();
        }

        private void CardHolder_TextChanged(object sender, TextChangedEventArgs e)
        {
            var tb = sender as TextBox;
            if (tb == null) return;

            _viewModel.CardHolder = tb.Text.Trim().ToUpper();
            UpdatePayButton();
        }

        private void Email_TextChanged(object sender, TextChangedEventArgs e)
        {
            var tb = sender as TextBox;
            if (tb == null) return;

            _viewModel.Email = tb.Text.Trim();
            UpdatePayButton();
        }

        private void UpdatePayButton()
        {
            BtnPay.IsEnabled = _viewModel.IsFormValid;
        }

        private async void BtnPay_Click(object sender, RoutedEventArgs e)
        {
            BtnPay.IsEnabled = false;
            BtnPay.Content = "⏳ Обработка...";

            try
            {
                var result = await BookingContext.PayBooking(_viewModel.Token, _viewModel.BookingId);

                if (result != null)
                {
                    ShowSuccess();
                }
                else
                {
                    ShowError("Ошибка обработки платежа. Попробуйте снова.");
                }
            }
            catch
            {
                ShowError("Ошибка соединения с платёжным шлюзом.");
            }
        }

        private void ShowSuccess()
        {
            // Скрываем все элементы формы
            var rootGrid = this.Content as Grid;
            if (rootGrid == null) return;

            // Скрываем ScrollViewer (он первый ребёнок Grid)
            var scrollViewer = rootGrid.Children[0] as ScrollViewer;
            if (scrollViewer != null)
                scrollViewer.Visibility = Visibility.Collapsed;

            // Показываем панель успеха
            SuccessEmailText.Text = _viewModel.Email;
            SuccessPanel.Visibility = Visibility.Visible;
        }
        private void BtnBackToBookings_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.Instanse?.MainFrame.Navigate(new MyBookingsPage(_viewModel.Token));
        }
        private void ShowError(string message)
        {
            MessageBox.Show(message, "Ошибка оплаты", MessageBoxButton.OK, MessageBoxImage.Error);
            BtnPay.IsEnabled = true;
            BtnPay.Content = "💳 Оплатить";
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.GoBack();
        }
    }
}