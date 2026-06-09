using RentAFriendApp.Context;
using RentAFriendApp.Models.ClassesDTO.BookingDTO;
using RentAFriendApp.Models.ClassesDTO.BookingDTO.Response;
using RentAFriendApp.ViewModels.Base;
using RentAFriendApp.ViewModels.Friend;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace RentAFriendApp.Views.Friend
{
    public partial class BookingRequestsPage : Page
    {
        private readonly string _token;
        private FriendBookingsViewModel _viewModel;

        public BookingRequestsPage(string token)
        {
            InitializeComponent();
            _token = token;

            _viewModel = new FriendBookingsViewModel(_token);
            DataContext = _viewModel;

            InitializeEvents();
            _ = InitializeAsync();
        }

        private async System.Threading.Tasks.Task InitializeAsync()
        {
            try
            {
                ShowLoadingState();

                await System.Threading.Tasks.Task.Delay(100);

                _viewModel.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(FriendBookingsViewModel.Items))
                    {
                        UpdateUI();
                    }
                };

                if (_viewModel.Items == null || !_viewModel.Items.Any())
                {
                    _viewModel.RefreshCommand.Execute(null);
                }

                ShowContentState();
            }
            catch (Exception ex)
            {
                ShowErrorState($"Ошибка инициализации: {ex.Message}");
            }
        }

        private void UpdateUI()
        {
            Dispatcher.Invoke(() =>
            {
                if (_viewModel.Items != null && _viewModel.Items.Any())
                {
                    BookingsListControl.ItemsSource = _viewModel.Items;
                    UpdateStatistics();
                    ShowContentState();
                }
                else
                {
                    ShowEmptyState();
                }
            });
        }

        private void UpdateStatistics()
        {
            if (_viewModel == null) return;

            PendingCountText.Text = _viewModel.PendingCount.ToString();
            ConfirmedCountText.Text = _viewModel.ConfirmedCount.ToString();
            TotalEarningsText.Text = $"{_viewModel.TotalEarnings:N0} ₽";

            _ = UpdateTotalClientsAsync();
        }

        private async Task UpdateTotalClientsAsync()
        {
            try
            {
                if (_viewModel.Items == null || !_viewModel.Items.Any())
                {
                    TotalClientsText.Text = "0";
                    return;
                }

                var uniqueClients = _viewModel.Items
                    .Select(b => b.ClientId)
                    .Distinct()
                    .Count();

                TotalClientsText.Text = uniqueClients.ToString();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка подсчета клиентов: {ex.Message}");
                TotalClientsText.Text = "0";
            }
        }

        private void InitializeEvents()
        {
            Messenger.Default.DataReceived += OnDataReceived;
            Messenger.Default.NotificationReceived += OnNotificationReceived;
        }

        // ========== МЕТОДЫ ОТОБРАЖЕНИЯ СОСТОЯНИЙ ==========
        private void ShowLoadingState()
        {
            LoadingState.Visibility = Visibility.Visible;
            BookingsListControl.Visibility = Visibility.Collapsed;
            EmptyState.Visibility = Visibility.Collapsed;
        }

        private void ShowContentState()
        {
            LoadingState.Visibility = Visibility.Collapsed;
            BookingsListControl.Visibility = Visibility.Visible;
            EmptyState.Visibility = Visibility.Collapsed;
        }

        private void ShowEmptyState()
        {
            LoadingState.Visibility = Visibility.Collapsed;
            BookingsListControl.Visibility = Visibility.Collapsed;
            EmptyState.Visibility = Visibility.Visible;
        }

        private void ShowErrorState(string message)
        {
            ShowNotification(message, "Error");
            ShowEmptyState();
        }

        private void ShowNotification(string message, string type = "Info")
        {
            Dispatcher.Invoke(() =>
            {
                var notification = new Border
                {
                    Background = type == "Error" ?
                        new SolidColorBrush(Colors.Red) :
                        new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50")),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(12),
                    Margin = new Thickness(0, 0, 0, 8)
                };

                var stackPanel = new StackPanel();
                var textBlock = new TextBlock
                {
                    Text = message,
                    Foreground = Brushes.White,
                    TextWrapping = TextWrapping.Wrap
                };

                stackPanel.Children.Add(textBlock);
                notification.Child = stackPanel;

                NotificationPanel.Child = notification;
                NotificationPanel.Visibility = Visibility.Visible;

                var timer = new System.Windows.Threading.DispatcherTimer();
                timer.Interval = TimeSpan.FromSeconds(3);
                timer.Tick += (s, e) =>
                {
                    NotificationPanel.Visibility = Visibility.Collapsed;
                    timer.Stop();
                };
                timer.Start();
            });
        }

        // ========== ОБРАБОТЧИКИ СОБЫТИЙ ==========
        private void OnDataReceived(object sender, object data)
        {
            if (data is string message && message == "BookingUpdated")
            {
                Dispatcher.Invoke(() =>
                {
                    _viewModel.RefreshCommand.Execute(null);
                    UpdateUI();
                });
            }
        }

        private void OnNotificationReceived(object sender, string message)
        {
            Dispatcher.Invoke(() =>
            {
                ShowNotification(message);
            });
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            BeginStoryboard((Storyboard)FindResource("FadeInAnimation"));
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            Messenger.Default.DataReceived -= OnDataReceived;
            Messenger.Default.NotificationReceived -= OnNotificationReceived;
        }
        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var rotateAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 360,
                    Duration = TimeSpan.FromSeconds(1),
                    RepeatBehavior = RepeatBehavior.Forever
                };

                var transform = new RotateTransform();
                RefreshButton.RenderTransform = transform;
                transform.BeginAnimation(RotateTransform.AngleProperty, rotateAnimation);

                _viewModel.RefreshCommand.Execute(null);
                UpdateUI();

                transform.BeginAnimation(RotateTransform.AngleProperty, null);
                transform.Angle = 0;

                ShowNotification("Данные обновлены");
            }
            catch (Exception ex)
            {
                ShowNotification($"Ошибка обновления: {ex.Message}", "Error");
            }
        }

        private void FilterButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag != null)
            {
                _viewModel.FilterByStatusCommand.Execute(button.Tag.ToString());

                var filterButtons = new[]
                {
                    AllFilterButton, PendingFilterButton, ConfirmedFilterButton,
                    CompletedFilterButton, CancelledFilterButton
                };

                foreach (var btn in filterButtons)
                {
                    if (btn == button)
                    {
                        btn.IsEnabled = false;
                        btn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
                        btn.Foreground = Brushes.White;
                    }
                    else
                    {
                        btn.IsEnabled = true;
                        btn.Background = new SolidColorBrush(Colors.Transparent);
                        btn.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#757575"));
                    }
                }
            }
        }

        // Обработчики действий с бронированиями
        private void AcceptBooking_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var booking = button?.DataContext as BookingDetailsDTO;

            if (booking != null)
            {
                _viewModel.AcceptBookingCommand.Execute(booking);
            }
        }

        private void RejectBooking_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var booking = button?.DataContext as BookingDetailsDTO;

            if (booking != null)
            {
                var result = MessageBox.Show(
                    "Вы уверены, что хотите отклонить этот запрос?",
                    "Подтверждение",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _viewModel.RejectBookingCommand.Execute(booking);
                }
            }
        }

        private void CompleteBooking_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var booking = button?.DataContext as BookingDetailsDTO;

            if (booking != null)
            {
                _viewModel.CompleteBookingCommand.Execute(booking);
            }
        }

        private async void ViewDetails_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var booking = button?.DataContext as BookingDetailsDTO;

            if (booking != null)
            {
                var details = await BookingContext.GetBookingDetails(_token, booking.BookingID);

                if (details?.Booking != null)
                {
                    var message = $@"
ID бронирования: #{details.Booking.BookingID}
Статус: {GetStatusDisplay(details.Booking.Status)}
Сумма: {details.Booking.TotalAmount:N0} ₽
Оплата: {GetPaymentStatusDisplay(details.Booking.PaymentStatus)}

Клиент:
  Имя: {details.Booking.ClientName}
  Email: {details.Booking.ClientEmail}
  Телефон: {details.Booking.ClientPhone}

Встреча:
  Дата: {details.Booking.ScheduleDate:dd.MM.yyyy}
  Время: {details.Booking.StartTime:hh\\:mm} - {details.Booking.EndTime:hh\\:mm}
  Место: {(string.IsNullOrEmpty(details.Booking.MeetingLocation) ? "Не указано" : details.Booking.MeetingLocation)}

Цель: {(string.IsNullOrEmpty(details.Booking.Purpose) ? "Не указана" : details.Booking.Purpose)}

Специальные пожелания:
{(string.IsNullOrEmpty(details.Booking.SpecialRequests) ? "Нет" : details.Booking.SpecialRequests)}

Создано: {details.Booking.CreatedAt:dd.MM.yyyy HH:mm}
Обновлено: {details.Booking.UpdatedAt:dd.MM.yyyy HH:mm}";

                    MessageBox.Show(message.Trim(), "Детали бронирования",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    ShowNotification("Не удалось загрузить детали бронирования", "Error");
                }
            }
        }

        private void MessageClient_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var booking = button?.DataContext as BookingDetailsDTO;

            if (booking != null)
            {
                _viewModel.MessageClientCommand.Execute(booking);
            }
        }

        private string GetStatusDisplay(string status)
        {
            return status switch
            {
                "Pending" => "Ожидает подтверждения",
                "Confirmed" => "Подтверждено",
                "Completed" => "Завершено",
                "Cancelled" => "Отменено",
                "Rejected" => "Отклонено",
                _ => status
            };
        }

        private string GetPaymentStatusDisplay(string paymentStatus)
        {
            return paymentStatus switch
            {
                "Paid" => "Оплачено",
                "Unpaid" => "Не оплачено",
                "Refunded" => "Возвращено",
                _ => paymentStatus
            };
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.Items == null || !_viewModel.Items.Any())
            {
                ShowNotification("Нет данных для экспорта", "Warning");
                return;
            }

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "CSV файлы (*.csv)|*.csv",
                DefaultExt = ".csv",
                FileName = $"Бронирования_{DateTime.Now:yyyy-MM-dd}"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    ExportToCsv(dialog.FileName, _viewModel.Items.ToList());
                    ShowNotification("Данные экспортированы", "Success");
                }
                catch (Exception ex)
                {
                    ShowNotification($"Ошибка экспорта: {ex.Message}", "Error");
                }
            }
        }

        private void ExportToCsv(string filePath, List<BookingDetailsDTO> bookings)
        {
            using (var writer = new System.IO.StreamWriter(filePath, false, System.Text.Encoding.UTF8))
            {
                writer.WriteLine("ID;Клиент;Email;Телефон;Статус;Дата;Время;Сумма;Оплата;Создано");

                foreach (var booking in bookings)
                {
                    writer.WriteLine($"{booking.BookingID};" +
                                   $"{booking.ClientName};" +
                                   $"{booking.ClientEmail};" +
                                   $"{booking.ClientPhone};" +
                                   $"{GetStatusDisplay(booking.Status)};" +
                                   $"{booking.ScheduleDate:dd.MM.yyyy};" +
                                   $"{booking.StartTime:hh\\:mm}-{booking.EndTime:hh\\:mm};" +
                                   $"{booking.TotalAmount:N0};" +
                                   $"{GetPaymentStatusDisplay(booking.PaymentStatus)};" +
                                   $"{booking.CreatedAt:dd.MM.yyyy HH:mm}");
                }
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Функция будет доступна в следующем обновлении", "Информация",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void CreateSchedule_Click(object sender, RoutedEventArgs e)
        {
            var schedulePage = new SchedulePage(_token);
            NavigationService?.Navigate(schedulePage);
        }

        private void TodayOnlyCheckBox_Changed(object sender, RoutedEventArgs e)
        {

        }

        private void PaidOnlyCheckBox_Changed(object sender, RoutedEventArgs e)
        {

        }
    }
}