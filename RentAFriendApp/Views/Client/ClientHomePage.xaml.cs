using RentAFriendApp.Models.ClassesDTO.FriendProfileDTO;
using RentAFriendApp.ViewModels.Client;
using RentAFriendApp.Views.Client;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Threading;

namespace RentAFriendApp.Views.Client
{
    public partial class ClientHomePage : Page
    {
        private ClientHomeViewModel? _viewModel;
        private string? _currentToken;
        private Border? _selectedBookingCard;
        private Border? _selectedFriendCard;
        private DispatcherTimer? _refreshTimer;

        public ClientHomePage(string token)
        {
            InitializeComponent();
            _currentToken = token;

            _viewModel = new ClientHomeViewModel(token);
            DataContext = _viewModel;

            // Запуск таймера для автообновления данных
            InitializeRefreshTimer();

            // Привязка событий
            Loaded += Page_Loaded;
            Unloaded += Page_Unloaded;
            SizeChanged += Page_SizeChanged;
        }

        private void InitializeRefreshTimer()
        {
            _refreshTimer = new DispatcherTimer();
            _refreshTimer.Interval = TimeSpan.FromMinutes(5);
            _refreshTimer.Tick += RefreshTimer_Tick;
            _refreshTimer.Start();
        }

        private void RefreshTimer_Tick(object sender, EventArgs e)
        {
            // Обновляем данные через ViewModel
            _viewModel?.RefreshCommand.Execute(null);
        }

        // ============= ОБРАБОТЧИКИ СОБЫТИЙ =============

        private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox searchBox && searchBox.Text == "Найти идеального компаньона...")
            {
                searchBox.Text = "";
                searchBox.Foreground = Brushes.Black;
            }
        }

        private void SearchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox searchBox && string.IsNullOrWhiteSpace(searchBox.Text))
            {
                searchBox.Text = "Найти идеального компаньона...";
                searchBox.Foreground = Brushes.Gray;
            }
        }

        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SearchButton_Click(sender, e);
            }
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            var searchBox = FindName("SearchTextBox") as TextBox;
            if (searchBox != null && !string.IsNullOrWhiteSpace(searchBox.Text) &&
                searchBox.Text != "Найти идеального компаньона...")
            {
                NavigateToSearch(searchBox.Text);
            }
        }

        private void QuickFilter_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is string filterType)
                {
                    // Анимация нажатия
                    var animation = (Storyboard)FindResource("ClickAnimation");
                    Storyboard.SetTarget(animation, button);
                    animation.Begin();

                    // Навигация с фильтром
                    NavigateToSearch("", filterType);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void RefreshRecommendations_Click(object sender, RoutedEventArgs e)
        {
            // Анимация вращения
            var rotateAnimation = new DoubleAnimation
            {
                From = 0,
                To = 360,
                Duration = TimeSpan.FromSeconds(0.5),
                RepeatBehavior = new RepeatBehavior(1)
            };

            var rotateTransform = new RotateTransform();
            RefreshRecommendationsBtn.RenderTransform = rotateTransform;
            rotateTransform.BeginAnimation(RotateTransform.AngleProperty, rotateAnimation);

            // Обновляем рекомендации через ViewModel
            _viewModel?.LoadRecommendedFriendsAsync();
        }

        private void StatsCard_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Border card)
            {
                card.Effect = new DropShadowEffect
                {
                    BlurRadius = 20,
                    Opacity = 0.15,
                    ShadowDepth = 3
                };
            }
        }

        private void StatsCard_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Border card)
            {
                var style = FindResource("StatsCardStyle") as Style;
                if (style != null)
                {
                    var effectSetter = style.Setters.OfType<Setter>()
                                          .FirstOrDefault(s => s.Property == Border.EffectProperty);
                    if (effectSetter != null)
                    {
                        card.Effect = effectSetter.Value as DropShadowEffect;
                    }
                }
            }
        }

        private void StatsCard_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border card && e.ChangedButton == MouseButton.Left)
            {
                string cardType = card.Tag as string;
                ShowDetailedStatistics(cardType);
            }
        }

        private void ShowDetailedStatistics(string cardType)
        {
            // Используем ViewModel для получения детальной статистики
            string title = "";
            string content = "";

            switch (cardType)
            {
                case "bookings":
                    title = "Детальная статистика по бронированиям";
                    content = GetBookingsStatistics();
                    break;
                case "spent":
                    title = "Финансовая статистика";
                    content = GetFinanceStatistics();
                    break;
                case "hours":
                    title = "Статистика по времени";
                    content = GetTimeStatistics();
                    break;
            }

            MessageBox.Show(content, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private string GetBookingsStatistics()
        {
            // В реальной реализации это бы делалось через ViewModel и процедуру
            return $"📊 Статистика бронирований\n\n" +
                   $"Всего бронирований: {_viewModel.TotalBookings}\n" +
                   $"Активных встреч: {_viewModel.ActiveBookings}\n" +
                   $"В этом месяце: {_viewModel.MonthlyCount} встреч\n" +
                   $"\nДля детальной статистики используется процедура sp_Client_GetUserStatistics";
        }

        private string GetFinanceStatistics()
        {
            return $"💰 Финансовая статистика\n\n" +
                   $"Всего потрачено: {_viewModel.TotalSpent:N0} ₽\n" +
                   $"Тренд: {_viewModel.SpentTrend}\n" +
                   $"\nДля детальной статистики используется процедура sp_Client_GetUserStatistics";
        }

        private string GetTimeStatistics()
        {
            return $"⏰ Статистика по времени\n\n" +
                   $"Всего часов с друзьями: {_viewModel.TotalHours} ч\n" +
                   $"Тренд: {_viewModel.HoursTrend}\n" +
                   $"\nДля детальной статистики используется процедура sp_Client_GetUserStatistics";
        }

        private void BookingCard_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border card && card.Tag is int bookingId)
            {
                if (e.ChangedButton == MouseButton.Left && e.ClickCount == 2)
                {
                    OpenBookingDetails(bookingId);
                }
                else if (e.ChangedButton == MouseButton.Left && e.ClickCount == 1)
                {
                    // Снимаем выделение с предыдущей карточки
                    if (_selectedBookingCard != null)
                    {
                        _selectedBookingCard.Background = Brushes.White;
                    }

                    // Выделяем текущую карточку
                    card.Background = new SolidColorBrush(Color.FromArgb(30, 76, 175, 80));
                    _selectedBookingCard = card;
                }
            }
        }

        private void FriendCard_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Border card) return;

            if (card.DataContext is not FPInfoDTO friend)
            {
                System.Diagnostics.Debug.WriteLine($"[FriendCard] DataContext не является FPInfoDTO: {card.DataContext?.GetType().Name}");
                return;
            }
            if (e.ChangedButton == MouseButton.Left && e.ClickCount == 1)
            {
                if (_selectedFriendCard != null)
                    _selectedFriendCard.Background = Brushes.White;

                card.Background = new SolidColorBrush(Color.FromArgb(30, 76, 175, 80));
                _selectedFriendCard = card;
            }
            else if (e.ChangedButton == MouseButton.Left && e.ClickCount == 2)
            {
                OpenFriendProfile(friend.ProfileID);
            }
        }

        private void NavigateToSearch(string searchText, string filter = "")
        {
            var searchParams = new
            {
                SearchText = searchText,
                Filter = filter,
            };

            // Сохраняем параметры поиска
            App.Current.Properties["SearchParams"] = searchParams;

            // Навигация к каталогу
            var catalogPage = new CatalogPage(_currentToken);
            MainWindow.Instanse?.MainFrame.Navigate(catalogPage);
        }

        private void OpenBookingDetails(int bookingId)
        {
            // Находим бронирование в коллекции ViewModel
            var booking = _viewModel.UpcomingBookings.FirstOrDefault(b => b.BookingID == bookingId);
            if (booking != null)
            {
                _viewModel.ViewBookingDetailsCommand.Execute(booking);
            }
        }

        private void OpenFriendProfile(int profileId)
        {
            if (profileId <= 0)
            {
                MessageBox.Show("Некорректный ID профиля.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var friendDetailsPage = new FriendDetailsPage(_currentToken, profileId);
                MainWindow.Instanse?.MainFrame.Navigate(friendDetailsPage);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Не удалось открыть профиль друга (ID={profileId}):\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                System.Diagnostics.Debug.WriteLine($"[OpenFriendProfile] Error: {ex}");
            }
        }

        private void AllMeetingsButton_Click(object sender, RoutedEventArgs e)
        {
            var myBookingsPage = new MyBookingsPage(_currentToken);
            MainWindow.Instanse?.MainFrame.Navigate(myBookingsPage);
        }

        private void ViewAllFriendsButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateToSearch("");
        }

        private void FindFriendButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel?.FindRandomFriendCommand.Execute(null);
        }

        private void CreateBookingButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var catalogPage = new CatalogPage(_currentToken);
                MainWindow.Instanse?.MainFrame.Navigate(catalogPage);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void LeaveReviewButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Чтобы оставить отзыв, выберите завершенную встречу в разделе 'Мои встречи'.",
                "Оставить отзыв", MessageBoxButton.OK, MessageBoxImage.Information);

            var myBookingsPage = new MyBookingsPage(_currentToken);
            MainWindow.Instanse?.MainFrame.Navigate(myBookingsPage);
        }

        private void FavoritesButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Раздел 'Избранное' в разработке.\n\nЗдесь будут отображаться ваши лучшие друзья.",
                "Избранное", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ProfileSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Настройки профиля клиента\n\nФункция в разработке.",
                "Настройки", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void MenuItem_Details_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is int bookingId)
            {
                OpenBookingDetails(bookingId);
            }
        }

        private void MenuItem_Cancel_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is int bookingId)
            {
                var result = MessageBox.Show("Вы уверены, что хотите отменить эту встречу?\n\nПри отмене менее чем за 24 часа может применяться штраф.",
                    "Отмена встречи", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    var booking = _viewModel.UpcomingBookings.FirstOrDefault(b => b.BookingID == bookingId);
                    if (booking != null)
                    {
                        _viewModel.CancelBookingCommand.Execute(booking);
                    }
                }
            }
        }

        private void MenuItem_Message_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is int bookingId)
            {
                //// Находим бронирование для получения ID друга
                //var booking = _viewModel.UpcomingBookings.FirstOrDefault(b => b.BookingID == bookingId);
                //if (booking != null && booking.FriendProfileID > 0)
                //{
                //    // Используем FriendProfileID для открытия чата
                //    // В реальной реализации нужно получить UserID друга из ProfileID
                //    _viewModel.OpenChatCommand.Execute(booking.FriendProfileID);
                //}
            }
        }


        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // Запускаем анимацию загрузки
            StartLoadingAnimation();
        }

        private void StartLoadingAnimation()
        {
            // Анимация появления контента
            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromSeconds(0.3)
            };

            var scrollViewer = VisualTreeHelper.GetChild(this, 0) as ScrollViewer;
            if (scrollViewer != null)
            {
                scrollViewer.BeginAnimation(OpacityProperty, fadeIn);
            }
        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.NewSize.Width < 1200)
            {
                // Можно добавить адаптивную верстку
                Grid.SetColumnSpan(FindName("SearchContainer") as Border, 2);
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            // Очистка ресурсов при выгрузке страницы
            if (_refreshTimer != null)
            {
                _refreshTimer.Stop();
                _refreshTimer = null;
            }
        }

        private void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            // Ленивая загрузка при прокрутке
            if (e.VerticalOffset + e.ViewportHeight >= e.ExtentHeight - 200)
            {
                // Можно загрузить дополнительные рекомендации
            }
        }

        // ============= ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ =============

        private string GetInitials(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return "??";

            var parts = fullName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                return $"{parts[0][0]}{parts[1][0]}".ToUpper();
            }
            else if (parts.Length == 1)
            {
                return parts[0].Length >= 2 ? parts[0].Substring(0, 2).ToUpper() : parts[0].ToUpper();
            }

            return "??";
        }

        private string GetTimeAgoText(DateTime createdAt)
        {
            var timeSpan = DateTime.Now - createdAt;

            if (timeSpan.TotalDays > 365)
                return $"{Math.Floor(timeSpan.TotalDays / 365)} лет назад";

            if (timeSpan.TotalDays > 30)
                return $"{Math.Floor(timeSpan.TotalDays / 30)} месяцев назад";

            if (timeSpan.TotalDays > 1)
                return $"{Math.Floor(timeSpan.TotalDays)} дней назад";

            if (timeSpan.TotalHours > 1)
                return $"{Math.Floor(timeSpan.TotalHours)} часов назад";

            if (timeSpan.TotalMinutes > 1)
                return $"{Math.Floor(timeSpan.TotalMinutes)} минут назад";

            return "только что";
        }

        private void BookingMenuButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}