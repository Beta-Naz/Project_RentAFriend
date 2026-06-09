using RentAFriendApp.Context;
using RentAFriendApp.Models;
using RentAFriendApp.Models.ClassesDTO.FriendProfileDTO;
using RentAFriendApp.Models.ClassesDTO.ReviewDTO;
using RentAFriendApp.ViewModels.Base;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace RentAFriendApp.ViewModels.Friend
{
    internal class FriendHomeViewModel : BaseViewModel
    {
        private readonly string _token;
        private int _currentUserId;
        private FPInfoDTO _currentProfile;

        // Статистика
        private int _totalBookings;
        public int TotalBookings
        {
            get => _totalBookings;
            set => SetProperty(ref _totalBookings, value);
        }

        private int _pendingRequests;
        public int PendingRequests
        {
            get => _pendingRequests;
            set => SetProperty(ref _pendingRequests, value);
        }

        private decimal _totalEarnings;
        public decimal TotalEarnings
        {
            get => _totalEarnings;
            set => SetProperty(ref _totalEarnings, value);
        }

        private double _averageRating;
        public double AverageRating
        {
            get => _averageRating;
            set
            {
                SetProperty(ref _averageRating, value);
                OnPropertyChanged(nameof(AverageRatingStars));
                OnPropertyChanged(nameof(RatingDescription));
            }
        }

        // Коллекции
        private ObservableCollection<UpcomingMeetingItem> _upcomingMeetings;
        public ObservableCollection<UpcomingMeetingItem> UpcomingMeetings
        {
            get => _upcomingMeetings;
            set => SetProperty(ref _upcomingMeetings, value);
        }

        private ObservableCollection<ReviewDTO> _recentReviews;
        public ObservableCollection<ReviewDTO> RecentReviews
        {
            get => _recentReviews;
            set => SetProperty(ref _recentReviews, value);
        }

        // UI свойства
        private string _welcomeMessage;
        public string WelcomeMessage
        {
            get => _welcomeMessage;
            set => SetProperty(ref _welcomeMessage, value);
        }

        private string _currentDate;
        public string CurrentDate
        {
            get => _currentDate;
            set => SetProperty(ref _currentDate, value);
        }

        private string _currentUserInitials;
        public string CurrentUserInitials
        {
            get => _currentUserInitials;
            set => SetProperty(ref _currentUserInitials, value);
        }

        private bool _hasUpcomingMeetings;
        public bool HasUpcomingMeetings
        {
            get => _hasUpcomingMeetings;
            set => SetProperty(ref _hasUpcomingMeetings, value);
        }

        private bool _hasRecentReviews;
        public bool HasRecentReviews
        {
            get => _hasRecentReviews;
            set => SetProperty(ref _hasRecentReviews, value);
        }

        private string _userFullName;
        public string UserFullName
        {
            get => _userFullName;
            set => SetProperty(ref _userFullName, value);
        }

        // Команды
        public ICommand EditProfileCommand { get; }
        public ICommand ManageScheduleCommand { get; }
        public ICommand ViewBookingRequestsCommand { get; }
        public ICommand RefreshCommand { get; }

        public FriendHomeViewModel(string token, int userId)
        {
            _token = token;
            _currentUserId = userId;
            Title = "Панель управления";

            UpcomingMeetings = new ObservableCollection<UpcomingMeetingItem>();
            RecentReviews = new ObservableCollection<ReviewDTO>();

            WelcomeMessage = "Добро пожаловать!";
            CurrentDate = DateTime.Now.ToString("dd MMMM yyyy");
            CurrentUserInitials = "??";

            EditProfileCommand = new RelayCommandAsync(EditProfile);
            ManageScheduleCommand = new RelayCommandAsync(ManageSchedule);
            ViewBookingRequestsCommand = new RelayCommandAsync(ViewBookingRequests);
            RefreshCommand = new RelayCommandAsync(LoadDataAsync);

            _ = LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                IsBusy = true;
                ClearErrors();

                await LoadProfileAsync();
                await LoadStatisticsAsync();
                await LoadUpcomingMeetingsAsync();
                await LoadRecentReviewsAsync();
                UpdateUIProperties();
            }
            catch (Exception ex)
            {
                SetError($"Ошибка загрузки данных: {ex.Message}");
                Base.Messenger.Default.SendNotification("Ошибка загрузки данных");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadProfileAsync()
        {
            try
            {
                // Получаем профиль друга
                var profilesResponse = await FriendProfileContext.GetAllProfiles(_token);
                _currentProfile = profilesResponse?.Profiles?.FirstOrDefault(p => p.UserID == _currentUserId);

                if (_currentProfile != null)
                {
                    // Получаем информацию о пользователе
                    var userInfo = await UserContext.GetUser(_token);
                    if (userInfo != null)
                    {
                        UserFullName = userInfo.FullName;
                        CurrentUserInitials = GetInitials(userInfo.FullName);
                        WelcomeMessage = $"Добро пожаловать, {userInfo.FullName}!";
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки профиля: {ex.Message}");
            }
        }

        private async Task LoadStatisticsAsync()
        {
            try
            {
                if (_currentProfile == null) return;

                // Получаем статистику бронирований друга
                var stats = await FriendProfileContext.GetFriendProfileStats(_token, _currentProfile.ProfileID);
                if (stats != null)
                {
                    TotalBookings = stats.TotalBookings;
                    TotalEarnings = stats.TotalEarnings;
                }

                // Получаем количество ожидающих запросов
                var pendingBookings = await BookingContext.GetMyBookings(_token, status: "Pending", page: 1, pageSize: 100);
                PendingRequests = pendingBookings?.Bookings?.Count ?? 0;

                // Получаем средний рейтинг
                AverageRating = _currentProfile.AverageRating.HasValue ? (double)_currentProfile.AverageRating.Value : 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки статистики: {ex.Message}");
            }
        }

        private async Task LoadUpcomingMeetingsAsync()
        {
            try
            {
                if (_currentProfile == null) return;

                await Application.Current.Dispatcher.InvokeAsync(() => UpcomingMeetings.Clear());

                // Получаем ближайшие встречи
                var upcoming = await FriendProfileContext.GetUpcomingMeetings(_token, _currentProfile.ProfileID, top: 5);

                if (upcoming?.Meetings != null)
                {
                    foreach (var meeting in upcoming.Meetings)
                    {
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            UpcomingMeetings.Add(meeting);
                        });
                    }
                }

                HasUpcomingMeetings = UpcomingMeetings.Count > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки ближайших встреч: {ex.Message}");
            }
        }

        private async Task LoadRecentReviewsAsync()
        {
            try
            {
                if (_currentProfile == null) return;

                await Application.Current.Dispatcher.InvokeAsync(() => RecentReviews.Clear());

                var reviews = await ReviewContext.GetReviewsByFriend(_currentProfile.ProfileID, _token, page: 1, pageSize: 5);

                if (reviews?.Reviews != null)
                {
                    foreach (var review in reviews.Reviews)
                    {
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            RecentReviews.Add(review);
                        });
                    }
                }

                HasRecentReviews = RecentReviews.Count > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки отзывов: {ex.Message}");
            }
        }

        private void UpdateUIProperties()
        {
            CurrentDate = DateTime.Now.ToString("dd MMMM yyyy");

            if (!string.IsNullOrEmpty(UserFullName))
            {
                CurrentUserInitials = GetInitials(UserFullName);
                WelcomeMessage = $"Добро пожаловать, {UserFullName}!";
            }
        }

        private string GetInitials(string fullName)
        {
            if (string.IsNullOrEmpty(fullName)) return "??";

            var parts = fullName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
                return (parts[0][0].ToString() + parts[1][0].ToString()).ToUpper();

            if (parts.Length == 1)
                return parts[0].Length >= 2 ? parts[0].Substring(0, 2).ToUpper() : parts[0].ToUpper();

            return "??";
        }

        private async Task EditProfile()
        {
            try
            {
                var editProfilePage = new EditProfilePage(_token);
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MainWindow.Instanse.MainFrame.Navigate(editProfilePage);
                });
            }
            catch (Exception ex)
            {
                SetError($"Ошибка открытия редактора профиля: {ex.Message}");
            }
        }

        private async Task ManageSchedule()
        {
            try
            {
                var schedulePage = new SchedulePage(_token);
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MainWindow.Instanse.MainFrame.Navigate(schedulePage);
                });
            }
            catch (Exception ex)
            {
                SetError($"Ошибка открытия расписания: {ex.Message}");
            }
        }

        private async Task ViewBookingRequests()
        {
            try
            {
                var bookingRequestsPage = new BookingRequestsPage(_token);
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MainWindow.Instanse.MainFrame.Navigate(bookingRequestsPage);
                });
            }
            catch (Exception ex)
            {
                SetError($"Ошибка открытия запросов: {ex.Message}");
            }
        }

        // Вычисляемые свойства для UI
        public string AverageRatingStars
        {
            get
            {
                if (AverageRating < 1 || AverageRating > 5)
                    return "☆☆☆☆☆";

                int fullStars = (int)Math.Floor(AverageRating);
                int halfStar = AverageRating - fullStars >= 0.5 ? 1 : 0;

                return new string('★', fullStars) +
                       (halfStar == 1 ? "½" : "") +
                       new string('☆', 5 - fullStars - halfStar);
            }
        }

        public string RatingDescription
        {
            get
            {
                if (AverageRating >= 4.5) return "Отлично";
                if (AverageRating >= 3.5) return "Хорошо";
                if (AverageRating >= 2.5) return "Нормально";
                if (AverageRating >= 1.5) return "Плохо";
                return "Ужасно";
            }
        }
    }
}