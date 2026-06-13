using RentAFriendApp.Context;
using RentAFriendApp.ViewModels.Base;
using RentAFriendApp.Views.Client;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Media;

namespace RentAFriendApp.ViewModels.Client
{
    internal class MyBookingsViewModel : BaseViewModel
    {
        private readonly string _token;
        public string Token => _token;

        #region СВОЙСТВА

        private ObservableCollection<BookingDisplayModel> _allBookings = new();
        private ObservableCollection<BookingDisplayModel> _filteredBookings = new();
        public ObservableCollection<BookingDisplayModel> FilteredBookings
        {
            get => _filteredBookings;
            set => SetProperty(ref _filteredBookings, value);
        }

        public bool IsEmpty => FilteredBookings.Count == 0;
        public bool IsNotEmpty => !IsEmpty;

        private int _totalBookings;
        public int TotalBookings
        {
            get => _totalBookings;
            set => SetProperty(ref _totalBookings, value);
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

        private decimal _averageCheck;
        public decimal AverageCheck
        {
            get => _averageCheck;
            set => SetProperty(ref _averageCheck, value);
        }

        public string BookingsCountText => $"({FilteredBookings.Count})";

        #endregion

        #region КОМАНДЫ
        public ICommand LoadBookingsCommand { get; }
        public ICommand FilterCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand CancelBookingCommand { get; }
        public ICommand OpenChatCommand { get; }
        public ICommand AddReviewCommand { get; }
        public ICommand ProcessPaymentCommand { get; }
        public ICommand RefreshCommand { get; }
        #endregion

        public MyBookingsViewModel(string token)
        {
            _token = token;

            LoadBookingsCommand = new RelayCommandAsync(LoadAllBookingsAsync);
            FilterCommand = new RelayCommandAsync<string>(ApplyFilter);
            SearchCommand = new RelayCommandAsync<string>(ApplySearch);
            CancelBookingCommand = new RelayCommandAsync<int>(CancelBookingAsync);
            OpenChatCommand = new RelayCommandAsync<int>(OpenChatAsync);
            AddReviewCommand = new RelayCommandAsync<int>(AddReview);
            ProcessPaymentCommand = new RelayCommandAsync<int>(ProcessPaymentAsync);
            RefreshCommand = new RelayCommandAsync(LoadAllBookingsAsync);

            _ = LoadAllBookingsAsync();
        }

        #region ЗАГРУЗКА
        private async Task LoadAllBookingsAsync()
        {
            try
            {
                IsBusy = true;
                ClearErrors();

                var resp = await BookingContext.GetMyBookings(_token, null, 1, int.MaxValue);
                if (resp?.Bookings == null) return;

                _allBookings.Clear();
                foreach (var b in resp.Bookings)
                {
                    _allBookings.Add(new BookingDisplayModel
                    {
                        BookingID = b.BookingID,
                        FriendProfileID = b.FriendId,
                        FriendName = b.FriendName,
                        FriendCity = b.FriendCity,
                        Purpose = b.Purpose,
                        Status = b.Status,
                        PaymentStatus = b.PaymentStatus,
                        TotalAmount = b.TotalAmount,
                        Date = b.ScheduleDate,
                        StartTime = b.StartTime,
                        EndTime = b.EndTime,
                        HasReview = b.HasReview
                    });
                }

                await LoadStatisticsAsync();
                ApplyFilter("All");
            }
            catch (Exception ex) { SetError(ex.Message); }
            finally { IsBusy = false; }
        }

        private async Task LoadStatisticsAsync()
        {
            var stats = await BookingContext.GetBookingStatistics(_token);
            if (stats == null) return;

            TotalBookings = stats.Statistics.TotalBookings;
            ActiveBookings = stats.Statistics.ActiveBookings;
            TotalSpent = stats.Statistics.TotalSpent;
            AverageCheck = stats.Statistics.AverageCheck;
        }
        #endregion

        #region ФИЛЬТРЫ
        private string _currentFilter = "All";
        private string _searchText = "";

        public Task ApplyFilter(string filter)
        {
            _currentFilter = filter ?? "All";
            DoFilter();
            return Task.CompletedTask;
        }

        public Task ApplySearch(string search)
        {
            _searchText = search ?? "";
            DoFilter();
            return Task.CompletedTask;
        }

        private void DoFilter()
        {
            var filtered = _allBookings.AsEnumerable();

            if (_currentFilter != "All")
                filtered = filtered.Where(b => b.Status == _currentFilter);

            if (!string.IsNullOrWhiteSpace(_searchText))
            {
                var s = _searchText.ToLower();
                filtered = filtered.Where(b =>
                    b.FriendName.ToLower().Contains(s) ||
                    b.Purpose.ToLower().Contains(s) ||
                    b.FriendCity.ToLower().Contains(s));
            }

            FilteredBookings = new ObservableCollection<BookingDisplayModel>(
                filtered.OrderByDescending(b => b.Date));
            OnPropertyChanged(nameof(IsEmpty));
            OnPropertyChanged(nameof(IsNotEmpty));
            OnPropertyChanged(nameof(BookingsCountText));
        }
        #endregion

        #region ДЕЙСТВИЯ

        private async Task CancelBookingAsync(int bookingId)
        {
            var booking = _allBookings.FirstOrDefault(b => b.BookingID == bookingId);
            if (booking == null) return;

            var result = System.Windows.MessageBox.Show(
                $"Отменить встречу с {booking.FriendName}?",
                "Отмена", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);

            if (result != System.Windows.MessageBoxResult.Yes) return;

            try
            {
                IsBusy = true;
                await BookingContext.CancelBooking(_token, bookingId);
                await LoadAllBookingsAsync();
            }
            catch (Exception ex) { SetError(ex.Message); }
            finally { IsBusy = false; }
        }

        public async Task OpenChatAsync(int friendProfileId)
        {
            try
            {
                var profile = await FriendProfileContext.GetFriendProfileById(friendProfileId, _token);
                if (profile?.Profile == null) return;

                var chat = await ChatContext.GetOrCreateChat(_token, profile.Profile.UserID);
                if (chat == null) return;

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    MainWindow.Instanse?.MainFrame.Navigate(
                        new ChatPage(_token, profile.Profile.UserID));
                });
            }
            catch (Exception ex) { SetError(ex.Message); }
        }

        private Task AddReview(int bookingId)
        {
            MainWindow.Instanse?.MainFrame.Navigate(new ReviewPage(_token, bookingId));
            return Task.CompletedTask;
        }

        private async Task ProcessPaymentAsync(int bookingId)
        {
            try
            {
                IsBusy = true;
                await BookingContext.PayBooking(_token, bookingId);
                await LoadAllBookingsAsync();
            }
            catch (Exception ex) { SetError(ex.Message); }
            finally { IsBusy = false; }
        }
        #endregion
    }

    #region МОДЕЛЬ ОТОБРАЖЕНИЯ
    public class BookingDisplayModel : BaseViewModel
    {
        private bool _hasReview;

        public int BookingID { get; set; }
        public int FriendProfileID { get; set; }
        public string FriendName { get; set; } = "";
        public string FriendCity { get; set; } = "";
        public string Purpose { get; set; } = "";
        public string Status { get; set; } = "Pending";
        public string PaymentStatus { get; set; } = "Unpaid";
        public decimal TotalAmount { get; set; }
        public DateTime Date { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }

        public bool HasReview
        {
            get => _hasReview;
            set
            {
                if (SetProperty(ref _hasReview, value))
                    OnPropertyChanged(nameof(CanReview));
            }
        }
        public TimeSpan Duration => EndTime - StartTime;
        public string FriendInitials => string.IsNullOrEmpty(FriendName) ? "?" : string.Concat(FriendName.Split(' ').Take(2).Select(w => w[0])).ToUpper();
        public bool CanBeCancelled => Status is "Pending" or "Confirmed";
        public bool CanReview => Status == "Completed" && !HasReview;
        public bool CanPay => PaymentStatus == "Unpaid" && Status != "Cancelled";
        public bool CanChat => Status is "Confirmed" or "Completed";
        public Color StatusBackground => Status switch
        {
            "Pending" => Color.FromRgb(255, 248, 225),
            "Confirmed" => Color.FromRgb(232, 245, 233),
            "Completed" => Color.FromRgb(227, 242, 253),
            _ => Color.FromRgb(250, 250, 250)
        };
    }
    #endregion
}