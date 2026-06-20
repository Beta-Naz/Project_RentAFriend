using RentAFriendApp.Context;
using RentAFriendApp.Models;
using RentAFriendApp.Models.ClassesDTO.FriendProfileDTO;
using RentAFriendApp.Models.ClassesDTO.UserDTO;
using RentAFriendApp.ViewModels.Base;
using RentAFriendApp.Views.Client;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace RentAFriendApp.ViewModels.Client
{
    internal class ClientHomeViewModel : BaseViewModel, IDisposable
    {
        private readonly string _token;
        private readonly DispatcherTimer _refreshTimer;
        private UserLoginDTO? _currentUser;

        #region Свойства

        // Пользователь
        private string _userFirstName = string.Empty;
        public string UserFirstName
        {
            get => _userFirstName;
            set => SetProperty(ref _userFirstName, value);
        }

        // Статистика
        private int _totalBookings;
        public int TotalBookings
        {
            get => _totalBookings;
            set
            {
                if (SetProperty(ref _totalBookings, value))
                    OnPropertyChanged(nameof(BookingsTrend));
            }
        }

        private int _activeBookings;
        public int ActiveBookings
        {
            get => _activeBookings;
            set => SetProperty(ref _activeBookings, value);
        }

        private int _completedCount;
        public int CompletedCount
        {
            get => _completedCount;
            set => SetProperty(ref _completedCount, value);
        }

        private decimal _totalSpent;
        public decimal TotalSpent
        {
            get => _totalSpent;
            set
            {
                if (SetProperty(ref _totalSpent, value))
                {
                    OnPropertyChanged(nameof(SpentTrend));
                    OnPropertyChanged(nameof(AverageCheck));
                }
            }
        }

        private int _totalHours;
        public int TotalHours
        {
            get => _totalHours;
            set
            {
                if (SetProperty(ref _totalHours, value))
                    OnPropertyChanged(nameof(HoursTrend));
            }
        }

        private int _monthlyCount;
        public int MonthlyCount
        {
            get => _monthlyCount;
            set => SetProperty(ref _monthlyCount, value);
        }

        private int _favoritesCount;
        public int FavoritesCount
        {
            get => _favoritesCount;
            set => SetProperty(ref _favoritesCount, value);
        }

        private int _uniqueFriendsCount;
        public int UniqueFriendsCount
        {
            get => _uniqueFriendsCount;
            set => SetProperty(ref _uniqueFriendsCount, value);
        }

        private int _lastMonthHours;
        public int LastMonthHours
        {
            get => _lastMonthHours;
            set => SetProperty(ref _lastMonthHours, value);
        }

        // Тренды
        public string BookingsTrend => CalculateTrend(TotalBookings, 0);
        public string SpentTrend => CalculateTrend(TotalSpent, 0m);
        public string HoursTrend => CalculateTrend(TotalHours, LastMonthHours);
        public string AverageCheck => TotalBookings > 0
            ? $"{(TotalSpent / TotalBookings):N0} ₽"
            : "0 ₽";

        // Видимость
        public bool HasNoUpcomingBookings => UpcomingBookings.Count == 0;
        public bool HasNoRecommendations => RecommendedFriends.Count == 0;

        // Коллекции
        private ObservableCollection<UpcomingBookingItem> _upcomingBookings = new();
        public ObservableCollection<UpcomingBookingItem> UpcomingBookings
        {
            get => _upcomingBookings;
            set
            {
                if (SetProperty(ref _upcomingBookings, value))
                    OnPropertyChanged(nameof(HasNoUpcomingBookings));
            }
        }

        private ObservableCollection<FPInfoDTO> _recommendedFriends = new();
        public ObservableCollection<FPInfoDTO> RecommendedFriends
        {
            get => _recommendedFriends;
            set
            {
                if (SetProperty(ref _recommendedFriends, value))
                    OnPropertyChanged(nameof(HasNoRecommendations));
            }
        }

        private ObservableCollection<RecentActivityDTO> _recentActivities = new();
        public ObservableCollection<RecentActivityDTO> RecentActivities
        {
            get => _recentActivities;
            set => SetProperty(ref _recentActivities, value);
        }
        #endregion

        #region Команды
        public ICommand RefreshCommand { get; }
        public ICommand RefreshRecommendationsCommand { get; }
        public ICommand NavigateToCatalogCommand { get; }
        public ICommand NavigateToMyBookingsCommand { get; }
        public ICommand ViewBookingDetailsCommand { get; }
        public ICommand CancelBookingCommand { get; }
        public ICommand OpenFriendProfileCommand { get; }
        public ICommand ShowDetailedStatisticsCommand { get; }
        public ICommand OpenProfileSettingsCommand { get; }
        public ICommand FindRandomFriend { get; }
        #endregion

        public ClientHomeViewModel(string token)
        {
            _token = token;
            Title = "Главная";

            FindRandomFriend = new RelayCommandAsync(NavigateToRandomProfile);
            RefreshCommand = new RelayCommandAsync(LoadDataAsync);
            RefreshRecommendationsCommand = new RelayCommandAsync(LoadRecommendedFriendsAsync);
            NavigateToCatalogCommand = new RelayCommand(NavigateToCatalog);
            NavigateToMyBookingsCommand = new RelayCommand(NavigateToMyBookings);
            ViewBookingDetailsCommand = new RelayCommand<int>(ViewBookingDetails);
            CancelBookingCommand = new RelayCommandAsync<int>(CancelBookingAsync);
            OpenFriendProfileCommand = new RelayCommand<int>(OpenFriendProfile);
            ShowDetailedStatisticsCommand = new RelayCommand<string>(ShowDetailedStatistics);
            OpenProfileSettingsCommand = new RelayCommand(OpenProfileSettings);

            // Таймер автообновления
            _refreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(5)
            };
            _refreshTimer.Tick += async (_, _) => await LoadDataAsync();
            _refreshTimer.Start();

            // Загрузка данных
            LoadDataAsync().ConfigureAwait(false);
        }

        #region Загрузка данных
        private async Task LoadDataAsync()
        {
            try
            {
                IsBusy = true;
                ClearErrors();

                var getUser = await UserContext.GetUser(_token);
                _currentUser = getUser?.Data;
                if (_currentUser == null) return;

                UpdateUserInfo(_currentUser);

                await Task.WhenAll(
                    LoadStatisticsAsync(),
                    LoadUpcomingBookingsAsync(),
                    LoadRecommendedFriendsAsync(),
                    LoadRecentActivitiesAsync()
                );
            }
            catch (Exception ex)
            {
                SetError($"Ошибка загрузки: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void UpdateUserInfo(UserLoginDTO user)
        {
            UserFirstName = user.FullName?.Split(' ').FirstOrDefault() ?? user.FullName ?? "Гость";
        }

        private async Task LoadStatisticsAsync()
        {
            var stats = await BookingContext.GetBookingStatistics(_token);
            if (stats == null) return;

            TotalBookings = stats.Statistics.TotalBookings;
            ActiveBookings = stats.Statistics.ActiveBookings;
            TotalSpent = stats.Statistics.TotalSpent;
            CompletedCount = stats.Statistics.CompletedBookings;

            var history = await BookingContext.GetMyBookings(_token, status: "completed", page: 1, pageSize: int.MaxValue);
            if (history?.Bookings == null) return;

            var now = DateTime.Now;

            MonthlyCount = history.Bookings.Count(b =>
                b.ScheduleDate.Month == now.Month && b.ScheduleDate.Year == now.Year);

            TotalHours = (int)history.Bookings.Sum(b =>
                (b.EndTime - b.StartTime).TotalHours);

            UniqueFriendsCount = history.Bookings
                .Select(b => b.FriendId)
                .Distinct()
                .Count();

            var lastMonth = now.AddMonths(-1);
            LastMonthHours = (int)history.Bookings
                .Where(b => b.ScheduleDate.Month == lastMonth.Month && b.ScheduleDate.Year == lastMonth.Year)
                .Sum(b => (b.EndTime - b.StartTime).TotalHours);
        }

        private async Task LoadUpcomingBookingsAsync()
        {
            var upcoming = await BookingContext.GetUpcomingBookings(_token, top: 5);
            if (upcoming?.Bookings == null) return;

            Application.Current.Dispatcher.Invoke(() =>
            {
                UpcomingBookings.Clear();
                foreach (var b in upcoming.Bookings)
                    UpcomingBookings.Add(b);
                OnPropertyChanged(nameof(HasNoUpcomingBookings));
            });
        }

        public async Task LoadRecommendedFriendsAsync()
        {
            var profiles = await FriendProfileContext.GetAllProfiles(_token);
            if (profiles?.Profiles == null) return;

            var top = profiles.Profiles
                .Where(p => p != null)
                .OrderByDescending(p => p.AverageRating)
                .Take(3)
                .ToList();

            Application.Current.Dispatcher.Invoke(() =>
            {
                RecommendedFriends.Clear();
                foreach (var p in top)
                    RecommendedFriends.Add(p);
                OnPropertyChanged(nameof(HasNoRecommendations));
            });
        }

        private async Task LoadRecentActivitiesAsync()
        {
            var active = await AuditLogContext.GetMyRecentLogs(_token);
            if (active?.Logs == null) return;

            Application.Current.Dispatcher.Invoke(() =>
            {
                RecentActivities.Clear();
                foreach (var l in active.Logs)
                {
                    if(l == null)
                    {
                        continue;
                    }
                    RecentActivities.Add(new RecentActivityDTO
                    {
                        Type = "Системные действия",
                        Name = l.TableName,
                        CreatedAt = l.LoggedAt,
                        Description = $"Действие: {l.NewValue?.Trim().ToLower()}."
                    });
                }
            });
        }
        #endregion

        #region Действия
        private async Task NavigateToRandomProfile()
        {
            var profiles = await FriendProfileContext.GetAllProfiles(_token);
            if (profiles?.Profiles == null) return;
            Random random = new Random();
            int idFriend = random.Next(0, profiles.Count);
            OpenProfileFriend(idFriend);
        }
        private async Task CancelBookingAsync(int bookingId)
        {
            var result = MessageBox.Show(
                "Вы уверены, что хотите отменить встречу?",
                "Отмена", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                IsBusy = true;
                await BookingContext.CancelBooking(_token, bookingId);
                await LoadUpcomingBookingsAsync();
                await LoadStatisticsAsync();
            }
            catch (Exception ex)
            {
                SetError($"Ошибка отмены: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ViewBookingDetails(int bookingId)
        {
            var booking = UpcomingBookings.FirstOrDefault(b => b.BookingID == bookingId);
            if (booking != null)
                Messenger.Default.SendData(booking);
        }

        private void OpenFriendProfile(int profileId)
        {
            if (profileId <= 0) return;

            try
            {
                var page = new FriendDetailsPage(_token, profileId);
                MainWindow.Instanse?.MainFrame.Navigate(page);
            }
            catch (Exception ex)
            {
                SetError($"Ошибка: {ex.Message}");
            }
        }

        private void ShowDetailedStatistics(string cardType)
        {
            var message = cardType switch
            {
                "bookings" => $"📊 Бронирования\n\nВсего: {TotalBookings}\nАктивных: {ActiveBookings}\nЗавершено: {CompletedCount}",
                "spent" => $"💰 Финансы\n\nПотрачено: {TotalSpent:N0} ₽\nСредний чек: {AverageCheck}",
                "hours" => $"⏰ Время\n\nВсего: {TotalHours} ч\nДрузей: {UniqueFriendsCount}",
                _ => "Статистика недоступна"
            };

            MessageBox.Show(message, "Статистика", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void NavigateToCatalog() =>
            MainWindow.Instanse?.MainFrame.Navigate(new CatalogPage(_token));

        private void NavigateToMyBookings() =>
            MainWindow.Instanse?.MainFrame.Navigate(new MyBookingsPage(_token));
        private void OpenProfileFriend(int idFriend) =>
              MainWindow.Instanse?.MainFrame.Navigate(new FriendDetailsPage(_token, idFriend));
        private void OpenProfileSettings() =>
            MessageBox.Show("Настройки профиля в разработке", "Настройки",
                MessageBoxButton.OK, MessageBoxImage.Information);
        #endregion

        #region Вспомогательные
        private string CalculateTrend(int current, int previous)
        {
            if (previous == 0) return current > 0 ? "↑" : "→";
            double pct = (current - previous) * 100.0 / previous;
            return pct > 0 ? $"↑ +{pct:F0}%" : pct < 0 ? $"↓ {pct:F0}%" : "→";
        }

        private string CalculateTrend(decimal current, decimal previous)
        {
            if (previous == 0) return current > 0 ? "↑" : "→";
            double pct = (double)((current - previous) * 100 / previous);
            return pct > 0 ? $"↑ +{pct:F0}%" : pct < 0 ? $"↓ {pct:F0}%" : "→";
        }

        public void Dispose()
        {
            _refreshTimer?.Stop();
        }
        #endregion
    }

    public class RecentActivityDTO
    {
        public string Type { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}