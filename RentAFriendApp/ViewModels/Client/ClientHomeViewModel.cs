using RentAFriendApp.Context;
using RentAFriendApp.Models;
using RentAFriendApp.Models.ClassesDTO.FriendProfileDTO;
using RentAFriendApp.Models.ClassesDTO.UserDTO;
using RentAFriendApp.ViewModels.Base;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace RentAFriendApp.ViewModels.Client
{
    internal class ClientHomeViewModel : BaseViewModel
    {
        private Random _random = new Random();
        private readonly string _token;

        // Детальная статистика
        private UserStatisticsDTO _userStatistics = new UserStatisticsDTO();

        // Основная статистика
        private int _totalBookings;
        public int TotalBookings
        {
            get => _totalBookings;
            set
            {
                if(SetProperty(ref _totalBookings, value))
                {
                    OnPropertyChanged(nameof(BookingsTrend));
                }
            }
        }

        private int _activeBookings;
        public int ActiveBookings
        {
            get => _activeBookings;
            set => SetProperty(ref _activeBookings, value);
        }

        private decimal _totalSpent;
        public decimal TotalSpent
        {
            get => _totalSpent;
            set => SetProperty(ref _totalSpent, value);
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

        private int _totalHours;
        public int TotalHours
        {
            get => _totalHours;
            set => SetProperty(ref _totalHours, value);
        }

        // Ближайшие бронирования
        private ObservableCollection<UpcomingBookingItem> _upcomingBookings;
        public ObservableCollection<UpcomingBookingItem> UpcomingBookings
        {
            get => _upcomingBookings;
            set => SetProperty(ref _upcomingBookings, value);
        }

        // Рекомендуемые друзья
        private ObservableCollection<FPInfoDTO> _recommendedFriends;
        public ObservableCollection<FPInfoDTO> RecommendedFriends
        {
            get => _recommendedFriends;
            set => SetProperty(ref _recommendedFriends, value);
        }

        // Недавняя активность
        private ObservableCollection<RecentActivityDTO> _recentActivities;
        public ObservableCollection<RecentActivityDTO> RecentActivities
        {
            get => _recentActivities;
            set => SetProperty(ref _recentActivities, value);
        }

        // Пользовательская информация
        private string _userFirstName;
        public string UserFirstName
        {
            get => _userFirstName;
            set => SetProperty(ref _userFirstName, value);
        }

        private string _userStatus;
        public string UserStatus
        {
            get => _userStatus;
            set => SetProperty(ref _userStatus, value);
        }

        private string _userFullName;
        public string UserFullName
        {
            get => _userFullName;
            set => SetProperty(ref _userFullName, value);
        }

        private DateTime _userCreatedAt;
        public DateTime UserCreatedAt
        {
            get => _userCreatedAt;
            set => SetProperty(ref _userCreatedAt, value);
        }

        private int _completedCount;
        public int CompletedCount
        {
            get => _completedCount;
            set => SetProperty(ref _completedCount, value);
        }

        // Тренды
        public string BookingsTrend =>
            _userStatistics.CalculateTrend(TotalBookings, 0);

        public string SpentTrend =>
            _userStatistics.CalculateTrend(TotalSpent, 0);

        public string HoursTrend =>
            _userStatistics.CalculateTrend(TotalHours, _userStatistics.LastMonthHours);

        // Команды
        public ICommand RefreshCommand { get; }
        public ICommand ViewBookingDetailsCommand { get; }
        public ICommand ViewFriendDetailsCommand { get; }
        public ICommand SearchFriendsCommand { get; }
        public ICommand CreateBookingCommand { get; }
        public ICommand CancelBookingCommand { get; }
        public ICommand FindRandomFriendCommand { get; }
        public ICommand OpenChatCommand { get; }

        public ClientHomeViewModel(string token)
        {
            _token = token;

            Title = "Главная";

            UpcomingBookings = new ObservableCollection<UpcomingBookingItem>();
            RecommendedFriends = new ObservableCollection<FPInfoDTO>();
            RecentActivities = new ObservableCollection<RecentActivityDTO>();

            // Инициализация команд
            RefreshCommand = new RelayCommandAsync(LoadDataAsync);
            ViewBookingDetailsCommand = new RelayCommandAsync<UpcomingBookingItem>(ViewBookingDetails);
            ViewFriendDetailsCommand = new RelayCommandAsync<FPInfoDTO>(ViewFriendDetails);
            SearchFriendsCommand = new RelayCommandAsync(SearchFriends);
            CreateBookingCommand = new RelayCommandAsync(CreateBooking);
            CancelBookingCommand = new RelayCommandAsync<UpcomingBookingItem>(CancelBookingAsync);
            FindRandomFriendCommand = new RelayCommandAsync(FindRandomFriendAsync);
            OpenChatCommand = new RelayCommandAsync<int>(OpenChatWithFriend);

            // Загрузка данных при инициализации
            LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                IsBusy = true;
                ClearErrors();
                var getUser = await UserContext.GetUser(_token);
                UserLoginDTO? user = getUser?.Data;
                if (user == null)
                {
                    return;
                }
                _userStatistics.UserID = user.UserID;

                UpdateUserInfo(user);

                // Загрузка детальной статистики
                await LoadUserStatisticsAsync();

                // Загрузка ближайших бронирований
                await LoadUpcomingBookingsAsync();

                // Загрузка рекомендаций
                await LoadRecommendedFriendsAsync();

                // Загрузка недавней активности
                await LoadRecentActivitiesAsync();
            }
            catch (Exception ex)
            {
                SetError($"Ошибка загрузки данных: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }
        private void UpdateUserInfo(UserLoginDTO user)
        {
            UserFullName = user.FullName;
            UserFirstName = user.FullName.Split(' ').FirstOrDefault() ?? user.FullName;

            if (CompletedCount >= 10)
                UserStatus = "Постоянный клиент";
            else if (CompletedCount >= 5)
                UserStatus = "Активный клиент";
            else
            {
                var monthsActive = (DateTime.UtcNow - UserCreatedAt).TotalDays / 30;
                UserStatus = monthsActive >= 6 ? "Начинающий" : "Новый клиент";
            }
        }

        private async Task LoadUserStatisticsAsync()
        {
            try
            {
                var bookingsStats = await BookingContext.GetBookingStatistics(_token);

                if (bookingsStats != null)
                {
                    TotalBookings = bookingsStats.Statistics.TotalBookings;
                    ActiveBookings = bookingsStats.Statistics.ActiveBookings;
                    TotalSpent = bookingsStats.Statistics.TotalSpent;
                    
                    var historyUser = await BookingContext.GetMyBookings(_token, null , 1, 20);
                    if (historyUser != null)
                    {
                        int monthlyCount = 0;
                        int totalHours = 0;
                        int currentMonth = DateTime.Now.Month;
                        int currentYear = DateTime.Now.Year;

                        foreach (var item in historyUser.Bookings)
                        {
                            if(item == null)
                            {
                                continue;
                            }
                            if (item.ScheduleDate.Month == currentMonth && item.ScheduleDate.Year == currentYear)
                            {
                                monthlyCount++;
                            }
                            var duration = item.EndTime - item.StartTime;
                            totalHours += (int)duration.TotalHours;
                        }
                        MonthlyCount = monthlyCount;
                        TotalHours = totalHours;
                    }
                }

                OnPropertyChanged(nameof(BookingsTrend));
                OnPropertyChanged(nameof(SpentTrend));
                OnPropertyChanged(nameof(HoursTrend));
            }
            catch (Exception ex)
            {
                SetError($"Ошибка загрузки статистики: {ex.Message}");
            }
        }

        private async Task LoadUpcomingBookingsAsync()
        {
            try
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => UpcomingBookings.Clear());

                var upcoming = await BookingContext.GetUpcomingBookings(_token, top: 5);
                if (upcoming != null && upcoming.Bookings != null)
                {
                    foreach (var booking in upcoming.Bookings)
                    {
                        await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                            UpcomingBookings.Add(booking)
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                SetError($"Ошибка загрузки ближайших бронирований: {ex.Message}");
            }
        }


        public async Task LoadRecommendedFriendsAsync()
        {
            try
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => RecommendedFriends.Clear());

                var profiles = await FriendProfileContext.GetAllProfiles(_token);
                if (profiles?.Profiles != null && profiles.Profiles.Any())
                {
                    var topProfiles = profiles.Profiles
                        .OrderByDescending(p => p.AverageRating)
                        .Take(10)
                        .ToList();

                    foreach (var profile in topProfiles)
                    {
                        if (profile != null)
                        {
                            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                                RecommendedFriends.Add(profile)
                            );
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SetError($"Ошибка загрузки рекомендуемых друзей: {ex.Message}");
            }
        }

        private async Task LoadRecentActivitiesAsync()
        {
            try
            {
                RecentActivities.Clear();

                // Получаем последние бронирования
                var bookings = await BookingContext.GetMyBookings(_token, status: "Completed", page: 1, pageSize: 5);
                if (bookings != null && bookings.Bookings != null)
                {
                    foreach (var booking in bookings.Bookings)
                    {
                        RecentActivities.Add(new RecentActivityDTO
                        {
                            Type = "booking",
                            FriendName = booking.FriendName,
                            CreatedAt = booking.CreatedAt,
                            Description = $"Завершена встреча с {booking.FriendName}"
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                SetError($"Ошибка загрузки недавней активности: {ex.Message}");
            }
        }

        private async Task CancelBookingAsync(UpcomingBookingItem booking)
        {
            if (booking == null) return;

            try
            {
                IsBusy = true;
                ClearErrors();

                var result = await BookingContext.CancelBooking(_token, booking.BookingID);
                if (result != null)
                {
                    Base.Messenger.Default.SendNotification($"Бронирование #{booking.BookingID} отменено");

                    // Обновляем данные
                    await LoadUpcomingBookingsAsync();
                    await LoadUserStatisticsAsync();
                }
                else
                {
                    SetError("Ошибка отмены бронирования");
                }
            }
            catch (Exception ex)
            {
                SetError($"Ошибка отмены бронирования: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task FindRandomFriendAsync()
        {
            try
            {
                IsBusy = true;
                ClearErrors();

                var profiles = await FriendProfileContext.GetAllProfiles(_token);
                int profileId = profiles?.Profiles[_random.Next(profiles.Profiles.Count)].ProfileID ?? -1;

                if (profileId <= 0)
                {
                    Base.Messenger.Default.SendNotification("Нет новых друзей для знакомства!");
                }
                Base.Messenger.Default.SendData(new { ProfileID = profileId });
                
            }
            catch (Exception ex)
            {
                SetError($"Ошибка поиска случайного друга: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private Task ViewBookingDetails(UpcomingBookingItem booking)
        {
            if (booking != null)
            {
                Base.Messenger.Default.SendData(booking);
            }
            return Task.CompletedTask;
        }

        private Task ViewFriendDetails(FPInfoDTO friend)
        {
            if (friend != null)
            {
                Base.Messenger.Default.SendData(friend);
            }
            return Task.CompletedTask;
        }

        private Task OpenChatWithFriend(int friendUserId)
        {
            Base.Messenger.Default.SendData(new { FriendUserId = friendUserId });
            return Task.CompletedTask;
        }

        private Task SearchFriends()
        {
            Base.Messenger.Default.SendNotification("Переход к поиску друзей");
            return Task.CompletedTask;
        }

        private Task CreateBooking()
        {
            Base.Messenger.Default.SendNotification("Создание нового бронирования");
            return Task.CompletedTask;
        }

        // Вспомогательные методы для UI
        public string GetInitials(string fullName)
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
    }

    // DTO для статистики
    public class UserStatisticsDTO
    {
        public int UserID { get; set; }
        public int TotalBookings { get; set; }
        public int ActiveBookings { get; set; }
        public decimal TotalSpent { get; set; }
        public int MonthlyCount { get; set; }
        public int FavoritesCount { get; set; }
        public int TotalHours { get; set; }
        public int LastMonthHours { get; set; }

        public string CalculateTrend(int current, int previous)
        {
            if (previous == 0) return current > 0 ? "↑ Рост" : "→ Нет данных";
            var percent = (current - previous) * 100.0 / previous;
            return percent > 0 ? $"↑ +{percent:F1}%" : percent < 0 ? $"↓ {percent:F1}%" : "→ Без изменений";
        }

        public string CalculateTrend(decimal current, decimal previous)
        {
            if (previous == 0) return current > 0 ? "↑ Рост" : "→ Нет данных";
            double percent = ((double)current - (double)previous) * 100.0 / (double)previous;
            return percent > 0 ? $"↑ +{percent:F1}%" : percent < 0 ? $"↓ {percent:F1}%" : "→ Без изменений";
        }
    }
    public class RecentActivityDTO
    {
        public string Type { get; set; } = string.Empty;
        public string FriendName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string Description { get; set; } = string.Empty;
        public int? Rating { get; set; }
    }
}