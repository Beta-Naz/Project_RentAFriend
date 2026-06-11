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
}