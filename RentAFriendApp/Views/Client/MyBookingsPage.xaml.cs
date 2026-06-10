using RentAFriendApp.Context;
using RentAFriendApp.Models.ClassesDTO.BookingDTO.Response;
using RentAFriendApp.ViewModels.Client;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace RentAFriendApp.Views.Client
{
    public partial class MyBookingsPage : Page
    {
        private readonly string _token;
        private MyBookingsViewModel _viewModel;
        private DispatcherTimer _searchTimer;

        public MyBookingsPage(string token)
        {
            InitializeComponent();
            _token = token;

            _viewModel = new MyBookingsViewModel(_token);
            DataContext = _viewModel;

            _searchTimer = new DispatcherTimer();
            _searchTimer.Interval = TimeSpan.FromMilliseconds(500);
            _searchTimer.Tick += SearchTimer_Tick;

            Loaded += Page_Loaded;

            // Подписка на изменение коллекции
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MyBookingsViewModel.FilteredBookings))
            {
                UpdateEmptyState();
            }
        }

        #region Таймер поиска

        private void SearchTimer_Tick(object sender, EventArgs e)
        {
            _searchTimer.Stop();
            _viewModel.SearchCommand.Execute(null);
        }

        #endregion

        #region Обработчики событий UI

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromSeconds(0.3)
            };
            this.BeginAnimation(OpacityProperty, fadeIn);

            _viewModel.LoadBookingsCommand.Execute(null);
        }

        private void FilterButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null) return;

            foreach (var btn in new[] { BtnFilterAll, BtnFilterPending, BtnFilterConfirmed, BtnFilterCompleted, BtnFilterCancelled })
            {
                btn.Tag = "";
            }

            button.Tag = "Active";

            string filterName = button.Content.ToString().Trim().ToLower();
            string filterStatus = MapFilterToStatus(filterName);

            _viewModel.FilterCommand.Execute(filterStatus);
        }

        private string MapFilterToStatus(string filterName)
        {
            switch (filterName)
            {
                case "ожидают":
                    return "Pending";
                case "подтверждены":
                    return "Confirmed";
                case "завершены":
                    return "Completed";
                case "отменены":
                    return "Cancelled";
                default:
                    return "All";
            }
        }

        private void SearchTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null && textBox.Text == textBox.Tag?.ToString())
            {
                textBox.Text = "";
                textBox.Foreground = Brushes.Black;
            }
        }

        private void SearchTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null && string.IsNullOrWhiteSpace(textBox.Text))
            {
                textBox.Text = textBox.Tag?.ToString();
                textBox.Foreground = Brushes.Gray;
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null && textBox.Text != textBox.Tag?.ToString())
            {
                _viewModel.SearchText = textBox.Text;
                textBox.Foreground = Brushes.Black;

                _searchTimer.Stop();
                _searchTimer.Start();
            }
        }

        private void BtnClearSearch_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = SearchTextBox.Tag?.ToString();
            SearchTextBox.Foreground = Brushes.Gray;
            _viewModel.SearchText = "";
            _viewModel.SearchCommand.Execute(null);
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            var rotation = new DoubleAnimation
            {
                From = 0,
                To = 360,
                Duration = TimeSpan.FromSeconds(0.5)
            };
            var transform = new RotateTransform();
            BtnRefresh.RenderTransform = transform;
            BtnRefresh.RenderTransformOrigin = new Point(0.5, 0.5);
            transform.BeginAnimation(RotateTransform.AngleProperty, rotation);

            _viewModel.RefreshCommand.Execute(null);
        }

        private void BookingCard_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var border = sender as Border;
            if (border?.Tag is int bookingId)
            {
                _viewModel.ShowDetailsCommand.Execute(bookingId);
            }
        }

        private void BtnDetails_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is int bookingId)
            {
                _viewModel.ShowDetailsCommand.Execute(bookingId);
            }
        }

        private void UpdateEmptyState()
        {
            bool hasBookings = _viewModel.FilteredBookings?.Count > 0;

            Application.Current.Dispatcher.Invoke(() =>
            {
                if (hasBookings)
                {
                    BookingsItemsControl.Visibility = Visibility.Visible;
                    EmptyState.Visibility = Visibility.Collapsed;
                }
                else
                {
                    BookingsItemsControl.Visibility = Visibility.Collapsed;
                    EmptyState.Visibility = Visibility.Visible;
                }
            });
        }

        private async void BtnChat_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is int friendProfileId)
            {
                try
                {
                    // Получаем профиль друга
                    var getProfile = await FriendProfileContext.GetFriendProfileById(friendProfileId, _token);
                    if (getProfile?.Profile != null)
                    {
                        var chat = await ChatContext.GetOrCreateChat(_token, getProfile.Profile.UserID);
                        if (chat != null)
                        {
                            var chatPage = new ChatPage(_token, getProfile.Profile.UserID);
                            MainWindow.Instanse.MainFrame.Navigate(chatPage);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка открытия чата: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is int bookingId)
            {
                var result = MessageBox.Show("Вы уверены, что хотите отменить это бронирование?",
                    "Подтверждение отмены",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _viewModel.CancelBookingCommand.Execute(bookingId);
                }
            }
        }

        private void BtnReview_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is int bookingId)
            {
                _viewModel.AddReviewCommand.Execute(bookingId);
            }
        }

        private void BtnPay_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is int bookingId)
            {
                _viewModel.ProcessPaymentCommand.Execute(bookingId);
            }
        }

        private void BtnFindFriends_Click(object sender, RoutedEventArgs e)
        {
            var catalog = new CatalogPage(_token);
            MainWindow.Instanse.MainFrame.Navigate(catalog);
        }

        #endregion
    }

    #region Модели для отображения

    public class BookingDisplayModel : INotifyPropertyChanged
    {
        public int BookingID { get; set; }
        public int FriendProfileID { get; set; }
        public string FriendName { get; set; } = string.Empty;
        public string FriendCity { get; set; } = string.Empty;
        public string Purpose { get; set; } = string.Empty;
        public string MeetingLocation { get; set; } = string.Empty;

        private string _status = "Pending";
        public string Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged(nameof(Status));
                OnPropertyChanged(nameof(CanBeCancelled));
                OnPropertyChanged(nameof(StatusForegroundColor));
            }
        }

        public string PaymentStatus { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public DateTime Date { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? SpecialRequests { get; set; }
        public bool HasReview { get; set; }
        public bool HasChat { get; set; }

        public string FriendInitials
        {
            get
            {
                if (string.IsNullOrEmpty(FriendName))
                    return "??";

                var parts = FriendName.Split(' ');
                if (parts.Length >= 2)
                    return $"{parts[0][0]}{parts[1][0]}".ToUpper();

                return FriendName.Length >= 2
                    ? FriendName.Substring(0, 2).ToUpper()
                    : FriendName.ToUpper();
            }
        }

        public DateTime StartDateTime => Date.Add(StartTime);
        public DateTime EndDateTime => Date.Add(EndTime);
        public TimeSpan Duration => EndTime - StartTime;

        public bool CanBeCancelled => Status == "Pending" || Status == "Confirmed";
        public bool CanChat => (Status == "Confirmed" || Status == "Completed") && !HasChat;
        public bool CanReview => Status == "Completed" && !HasReview;
        public bool CanPay => PaymentStatus == "Unpaid" && Status != "Cancelled" && Status != "Rejected";

        public string PaymentStatusDisplay
        {
            get
            {
                switch (PaymentStatus)
                {
                    case "Paid": return "Оплачено";
                    case "Unpaid": return "Не оплачено";
                    case "Refunded": return "Возвращено";
                    default: return PaymentStatus;
                }
            }
        }

        public Brush StatusForegroundColor
        {
            get
            {
                switch (Status)
                {
                    case "Pending": return new SolidColorBrush(Color.FromRgb(255, 152, 0)); // Orange
                    case "Confirmed": return new SolidColorBrush(Color.FromRgb(76, 175, 80)); // Green
                    case "Completed": return new SolidColorBrush(Color.FromRgb(33, 150, 243)); // Blue
                    case "Cancelled": return new SolidColorBrush(Color.FromRgb(244, 67, 54)); // Red
                    case "Rejected": return new SolidColorBrush(Color.FromRgb(244, 67, 54)); // Red
                    default: return Brushes.Gray;
                }
            }
        }

        public Brush StatusBackgroundColor
        {
            get
            {
                switch (Status)
                {
                    case "Pending": return new SolidColorBrush(Color.FromRgb(255, 248, 225));
                    case "Confirmed": return new SolidColorBrush(Color.FromRgb(232, 245, 233));
                    case "Completed": return new SolidColorBrush(Color.FromRgb(227, 242, 253));
                    case "Cancelled": return new SolidColorBrush(Color.FromRgb(253, 237, 237));
                    case "Rejected": return new SolidColorBrush(Color.FromRgb(253, 237, 237));
                    default: return Brushes.LightGray;
                }
            }
        }

        public Brush PaymentStatusColor
        {
            get
            {
                switch (PaymentStatus)
                {
                    case "Paid": return new SolidColorBrush(Color.FromRgb(76, 175, 80));
                    case "Unpaid": return new SolidColorBrush(Color.FromRgb(244, 67, 54));
                    case "Refunded": return new SolidColorBrush(Color.FromRgb(33, 150, 243));
                    default: return Brushes.Gray;
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    #endregion
}