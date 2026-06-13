using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using RentAFriendApp.Context;
using RentAFriendApp.Models.ClassesDTO.BookingDTO;
using RentAFriendApp.ViewModels.Base;

namespace RentAFriendApp.ViewModels.Friend
{
    internal class FriendBookingsViewModel : BaseViewModel
    {
        private readonly string _token;
        public string Token => _token;
        private int _profileId;

        private ObservableCollection<FriendBookingDisplayModel> _items = new();
        public ObservableCollection<FriendBookingDisplayModel> Items
        {
            get => _items;
            set => SetProperty(ref _items, value);
        }

        private string _selectedStatus = "All";
        public string SelectedStatus
        {
            get => _selectedStatus;
            set
            {
                if (SetProperty(ref _selectedStatus, value))
                    _ = LoadItemsAsync();
            }
        }

        public int PendingCount => Items.Count(b => b.Status == "Pending");
        public int ConfirmedCount => Items.Count(b => b.Status == "Confirmed");
        public decimal TotalEarnings => Items.Where(b => b.PaymentStatus == "Paid" && b.Status == "Completed").Sum(b => b.TotalAmount);
        public int TotalClients => Items.Select(b => b.ClientId).Distinct().Count();
        public int FilteredCount => Items.Count;
        public bool IsEmpty => Items.Count == 0;
        public bool IsNotEmpty => !IsEmpty;

        public ICommand RefreshCommand { get; }
        public ICommand FilterCommand { get; }
        public ICommand AcceptCommand { get; }
        public ICommand RejectCommand { get; }
        public ICommand CompleteCommand { get; }
        public ICommand ChatCommand { get; }

        public FriendBookingsViewModel(string token)
        {
            _token = token;
            Title = "Запросы на бронирование";

            RefreshCommand = new RelayCommandAsync(LoadItemsAsync);
            FilterCommand = new RelayCommand<string>(status => SelectedStatus = status);
            AcceptCommand = new RelayCommandAsync<int>(AcceptAsync);
            RejectCommand = new RelayCommandAsync<int>(RejectAsync);
            CompleteCommand = new RelayCommandAsync<int>(CompleteAsync);
            ChatCommand = new RelayCommandAsync<int>(OpenChatAsync);

            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            var user = await UserContext.GetUser(_token);
            var profiles = await FriendProfileContext.GetAllProfiles(_token);
            var profile = profiles?.Profiles?.FirstOrDefault(p => p.UserID == user?.Data?.UserID);
            if (profile != null) _profileId = profile.ProfileID;

            await LoadItemsAsync();
        }

        public async Task LoadItemsAsync()
        {
            try
            {
                IsBusy = true;
                ClearErrors();

                if (_profileId <= 0) return;

                string? filter = SelectedStatus == "All" ? null : SelectedStatus;
                var bookings = await BookingContext.GetFriendBookings(_token, _profileId, filter);

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Items.Clear();
                    if (bookings?.Bookings != null)
                    {
                        foreach (var b in bookings.Bookings)
                            Items.Add(new FriendBookingDisplayModel(b));
                    }
                });

                OnPropertyChanged(nameof(PendingCount));
                OnPropertyChanged(nameof(ConfirmedCount));
                OnPropertyChanged(nameof(TotalEarnings));
                OnPropertyChanged(nameof(TotalClients));
                OnPropertyChanged(nameof(FilteredCount));
                OnPropertyChanged(nameof(IsEmpty));
                OnPropertyChanged(nameof(IsNotEmpty));
            }
            catch (Exception ex) { SetError(ex.Message); }
            finally { IsBusy = false; }
        }

        private async Task AcceptAsync(int bookingId)
        {
            await UpdateStatus(bookingId, "Confirmed", "подтверждено");
        }

        private async Task RejectAsync(int bookingId)
        {
            if (MessageBox.Show("Отклонить запрос?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            await UpdateStatus(bookingId, "Rejected", "отклонено");
        }

        private async Task CompleteAsync(int bookingId)
        {
            await UpdateStatus(bookingId, "Completed", "завершена");
        }

        private async Task UpdateStatus(int bookingId, string status, string message)
        {
            try
            {
                IsBusy = true;
                await BookingContext.UpdateBookingStatus(_token, bookingId, status);
                Messenger.Default.SendNotification($"Бронирование #{bookingId} {message}");
                await LoadItemsAsync();
            }
            catch (Exception ex) { SetError(ex.Message); }
            finally { IsBusy = false; }
        }

        private async Task OpenChatAsync(int bookingId)
        {
            var booking = Items.FirstOrDefault(b => b.BookingID == bookingId);
            if (booking == null) return;

            var chat = await ChatContext.GetOrCreateChat(_token, booking.ClientId);
            if (chat != null)
            {
                MessageBox.Show("Будет добавлена в следующем обновлении");
            }
        }
    }

    public class FriendBookingDisplayModel : BaseViewModel
    {
        private int _bookingID;
        public int BookingID
        {
            get => _bookingID;
            set => SetProperty(ref _bookingID, value);
        }

        private int _clientId;
        public int ClientId
        {
            get => _clientId;
            set => SetProperty(ref _clientId, value);
        }

        private string _clientName = "";
        public string ClientName
        {
            get => _clientName;
            set
            {
                if (SetProperty(ref _clientName, value))
                    OnPropertyChanged(nameof(ClientInitials));
            }
        }

        private string _clientEmail = "";
        public string ClientEmail
        {
            get => _clientEmail;
            set => SetProperty(ref _clientEmail, value);
        }

        private string _clientPhone = "";
        public string ClientPhone
        {
            get => _clientPhone;
            set => SetProperty(ref _clientPhone, value);
        }

        private string _status = "";
        public string Status
        {
            get => _status;
            set
            {
                if (SetProperty(ref _status, value))
                {
                    OnPropertyChanged(nameof(StatusDisplay));
                    OnPropertyChanged(nameof(StatusBackground));
                    OnPropertyChanged(nameof(CanAccept));
                    OnPropertyChanged(nameof(CanReject));
                    OnPropertyChanged(nameof(CanComplete));
                }
            }
        }

        private string _paymentStatus = "";
        public string PaymentStatus
        {
            get => _paymentStatus;
            set
            {
                if (SetProperty(ref _paymentStatus, value))
                    OnPropertyChanged(nameof(PaymentStatusDisplay));
            }
        }

        private decimal _totalAmount;
        public decimal TotalAmount
        {
            get => _totalAmount;
            set => SetProperty(ref _totalAmount, value);
        }

        private DateTime _scheduleDate;
        public DateTime ScheduleDate
        {
            get => _scheduleDate;
            set
            {
                if (SetProperty(ref _scheduleDate, value))
                    OnPropertyChanged(nameof(DateDisplay));
            }
        }

        private TimeSpan _startTime;
        public TimeSpan StartTime
        {
            get => _startTime;
            set
            {
                if (SetProperty(ref _startTime, value))
                    OnPropertyChanged(nameof(TimeRange));
            }
        }

        private TimeSpan _endTime;
        public TimeSpan EndTime
        {
            get => _endTime;
            set
            {
                if (SetProperty(ref _endTime, value))
                {
                    OnPropertyChanged(nameof(TimeRange));
                    OnPropertyChanged(nameof(Duration));
                }
            }
        }

        private string _meetingLocation = "";
        public string MeetingLocation
        {
            get => _meetingLocation;
            set => SetProperty(ref _meetingLocation, value);
        }

        private string _purpose = "";
        public string Purpose
        {
            get => _purpose;
            set => SetProperty(ref _purpose, value);
        }

        private DateTime _createdAt;
        public DateTime CreatedAt
        {
            get => _createdAt;
            set => SetProperty(ref _createdAt, value);
        }

        // Вычисляемые свойства (только для чтения)
        public string ClientInitials => string.IsNullOrEmpty(ClientName)
            ? "?"
            : string.Concat(ClientName.Split(' ').Take(2).Select(w => w[0])).ToUpper();

        public string DateDisplay => ScheduleDate.ToString("dd.MM.yyyy");
        public string TimeRange => $"{StartTime:hh\\:mm} – {EndTime:hh\\:mm}";
        public string Duration => $"{(EndTime - StartTime).TotalHours:F1} ч";

        public string StatusDisplay => Status switch
        {
            "Pending" => "Ожидает",
            "Confirmed" => "Подтверждена",
            "Completed" => "Завершена",
            "Cancelled" => "Отменена",
            "Rejected" => "Отклонена",
            _ => Status
        };

        public string PaymentStatusDisplay => PaymentStatus == "Paid" ? "Оплачено" : "Не оплачено";

        public bool CanAccept => Status == "Pending";
        public bool CanReject => Status == "Pending";
        public bool CanComplete => Status == "Confirmed";

        public Color StatusBackground => Status switch
        {
            "Pending" => Color.FromRgb(255, 248, 225),
            "Confirmed" => Color.FromRgb(232, 245, 233),
            "Completed" => Color.FromRgb(227, 242, 253),
            "Rejected" => Color.FromRgb(255, 235, 238),
            _ => Color.FromRgb(250, 250, 250)
        };

        public FriendBookingDisplayModel(){}

        public FriendBookingDisplayModel(BookingDetailsDTO dto)
        {
            BookingID = dto.BookingID;
            ClientId = dto.ClientId;
            ClientName = dto.ClientName;
            ClientEmail = dto.ClientEmail;
            ClientPhone = dto.ClientPhone;
            Status = dto.Status;
            PaymentStatus = dto.PaymentStatus;
            TotalAmount = dto.TotalAmount;
            ScheduleDate = dto.ScheduleDate;
            StartTime = dto.StartTime;
            EndTime = dto.EndTime;
            MeetingLocation = dto.MeetingLocation ?? "Не указано";
            Purpose = dto.Purpose ?? "Не указана";
            CreatedAt = dto.CreatedAt;
        }
    }
}