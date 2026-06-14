using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using RentAFriendApp.Context;
using RentAFriendApp.Models;
using RentAFriendApp.Models.ClassesDTO.FriendProfileDTO;
using RentAFriendApp.Models.ClassesDTO.ReviewDTO;
using RentAFriendApp.ViewModels.Base;
using RentAFriendApp.Views.Friend;

namespace RentAFriendApp.ViewModels.Friend
{
    internal class FriendHomeViewModel : BaseViewModel
    {
        private readonly string _token;
        private FPInfoDTO? _currentProfile;

        #region Свойства

        private int _totalBookings;
        public int TotalBookings { get => _totalBookings; set => SetProperty(ref _totalBookings, value); }

        private int _pendingRequests;
        public int PendingRequests { get => _pendingRequests; set => SetProperty(ref _pendingRequests, value); }

        private decimal _totalEarnings;
        public decimal TotalEarnings { get => _totalEarnings; set => SetProperty(ref _totalEarnings, value); }

        private double _averageRating;
        public double AverageRating
        {
            get => _averageRating;
            set { SetProperty(ref _averageRating, value); OnPropertyChanged(nameof(AverageRatingStars)); }
        }

        private ObservableCollection<UpcomingMeetingItem> _upcomingMeetings = new();
        public ObservableCollection<UpcomingMeetingItem> UpcomingMeetings
        {
            get => _upcomingMeetings;
            set => SetProperty(ref _upcomingMeetings, value);
        }

        private ObservableCollection<ReviewDTO> _recentReviews = new();
        public ObservableCollection<ReviewDTO> RecentReviews
        {
            get => _recentReviews;
            set => SetProperty(ref _recentReviews, value);
        }

        private string _welcomeMessage = "Добро пожаловать!";
        public string WelcomeMessage { get => _welcomeMessage; set => SetProperty(ref _welcomeMessage, value); }

        private string _currentDate = DateTime.Now.ToString("dd MMMM yyyy");
        public string CurrentDate { get => _currentDate; set => SetProperty(ref _currentDate, value); }

        private string _currentUserInitials = "??";
        public string CurrentUserInitials { get => _currentUserInitials; set => SetProperty(ref _currentUserInitials, value); }

        public bool HasUpcomingMeetings => UpcomingMeetings.Count > 0;
        public bool HasRecentReviews => RecentReviews.Count > 0;

        public ICommand EditProfileCommand { get; }
        public ICommand ManageScheduleCommand { get; }
        public ICommand ViewBookingRequestsCommand { get; }
        public ICommand RefreshCommand { get; }

        public string AverageRatingStars
        {
            get
            {
                int full = (int)Math.Floor(_averageRating);
                bool half = _averageRating - full >= 0.5;
                return new string('★', full) + (half ? "½" : "") + new string('☆', 5 - full - (half ? 1 : 0));
            }
        }

        #endregion

        public FriendHomeViewModel(string token)
        {
            _token = token;
            Title = "Панель управления";

            EditProfileCommand = new RelayCommandAsync(EditProfile);
            ManageScheduleCommand = new RelayCommandAsync(ManageSchedule);
            ViewBookingRequestsCommand = new RelayCommandAsync(ViewBookingRequests);
            RefreshCommand = new RelayCommandAsync(LoadDataAsync);

            _ = LoadDataAsync(); // Только один вызов
        }

        private async Task LoadDataAsync()
        {
            try
            {
                IsBusy = true;
                ClearErrors();

                await LoadProfileAsync();
                if (_currentProfile == null) return;

                await Task.WhenAll(
                    LoadStatisticsAsync(),
                    LoadUpcomingMeetingsAsync(),
                    LoadRecentReviewsAsync()
                );

                UpdateUI();
            }
            catch (Exception ex) { SetError(ex.Message); }
            finally { IsBusy = false; }
        }

        private async Task LoadProfileAsync()
        {
            var resp = await FriendProfileContext.GetMyProfile(_token);
            _currentProfile = resp?.Profile;

            if (_currentProfile != null)
            {
                var user = await UserContext.GetUser(_token);
                var name = user?.Data?.FullName ?? "Друг";
                WelcomeMessage = $"Добро пожаловать, {name}!";
                CurrentUserInitials = GetInitials(name);
            }
        }

        private async Task LoadStatisticsAsync()
        {
            if (_currentProfile == null) return;

            var stats = await FriendProfileContext.GetFriendProfileStats(_token, _currentProfile.ProfileID);
            if (stats?.Statistic != null)
            {
                TotalBookings = stats.Statistic.TotalBookings;
                TotalEarnings = stats.Statistic.TotalEarnings;
            }

            var pending = await BookingContext.GetMyBookings(_token, status: "Pending", page: 1, pageSize: 100);
            PendingRequests = pending?.Bookings?.Count ?? 0;

            AverageRating = (double)(_currentProfile.AverageRating ?? 0m);
        }

        private async Task LoadUpcomingMeetingsAsync()
        {
            if (_currentProfile == null) return;

            var upcoming = await FriendProfileContext.GetUpcomingMeetings(_token, _currentProfile.ProfileID, top: 5);
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                UpcomingMeetings.Clear();
                if (upcoming?.Meetings != null)
                    foreach (var m in upcoming.Meetings)
                        UpcomingMeetings.Add(m);
                OnPropertyChanged(nameof(HasUpcomingMeetings));
            });
        }

        private async Task LoadRecentReviewsAsync()
        {
            if (_currentProfile == null) return;

            var reviews = await ReviewContext.GetReviewsByFriend(_currentProfile.ProfileID, _token, page: 1, pageSize: 5);
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                RecentReviews.Clear();
                if (reviews?.Reviews != null)
                    foreach (var r in reviews.Reviews)
                        RecentReviews.Add(r);
                OnPropertyChanged(nameof(HasRecentReviews));
            });
        }

        private void UpdateUI()
        {
            CurrentDate = DateTime.Now.ToString("dd MMMM yyyy");
            OnPropertyChanged(nameof(HasUpcomingMeetings));
            OnPropertyChanged(nameof(HasRecentReviews));
        }

        private string GetInitials(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "??";
            var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length >= 2 ? $"{parts[0][0]}{parts[1][0]}".ToUpper() : name[..Math.Min(2, name.Length)].ToUpper();
        }

        private async Task EditProfile() => Navigate(new EditProfilePage(_token));
        private async Task ManageSchedule() => Navigate(new SchedulePage(_token));
        private async Task ViewBookingRequests() => Navigate(new BookingRequestsPage(_token));

        private async Task Navigate(Page page)
        {
            await Application.Current.Dispatcher.InvokeAsync(() => MainWindow.Instanse?.MainFrame.Navigate(page));
        }
        public string RatingDescription => _averageRating switch
        {
            >= 4.5 => "Отлично",
            >= 3.5 => "Хорошо",
            >= 2.5 => "Нормально",
            >= 1.5 => "Плохо",
            > 0 => "Ужасно",
            _ => "Нет оценок"
        };
    }
}