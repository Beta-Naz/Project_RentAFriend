using RentAFriendApp.Context;
using RentAFriendApp.Models.ClassesDTO.BookingDTO.Response;
using RentAFriendApp.ViewModels.Base;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace RentAFriendApp.ViewModels.Client
{
    internal class MyBookingsViewModel : BaseViewModel
    {
        private readonly string _token;

        // Данные
        private ObservableCollection<BookingDisplayModel> _bookings;
        public ObservableCollection<BookingDisplayModel> Bookings
        {
            get => _bookings;
            set => SetProperty(ref _bookings, value);
        }

        private ObservableCollection<BookingDisplayModel> _filteredBookings;
        public ObservableCollection<BookingDisplayModel> FilteredBookings
        {
            get => _filteredBookings;
            set => SetProperty(ref _filteredBookings, value);
        }

        private string _currentFilter = "All";
        public string CurrentFilter
        {
            get => _currentFilter;
            set
            {
                if (SetProperty(ref _currentFilter, value))
                {
                    ApplyFilters();
                }
            }
        }

        private string _searchText = "";
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    ApplyFilters();
                }
            }
        }

        // Статистика
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

        // Команды
        public ICommand LoadBookingsCommand { get; }
        public ICommand FilterCommand { get; }
        public ICommand ShowDetailsCommand { get; }
        public ICommand CancelBookingCommand { get; }
        public ICommand OpenChatCommand { get; }
        public ICommand AddReviewCommand { get; }
        public ICommand ProcessPaymentCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand RefreshCommand { get; }

        public MyBookingsViewModel(string token)
        {
            _token = token;

            Bookings = new ObservableCollection<BookingDisplayModel>();
            FilteredBookings = new ObservableCollection<BookingDisplayModel>();

            // Инициализация команд
            LoadBookingsCommand = new RelayCommandAsync(LoadBookingsAsync);
            FilterCommand = new RelayCommandAsync<string>(ApplyFilter);
            ShowDetailsCommand = new RelayCommandAsync<int>(ShowBookingDetailsAsync);
            CancelBookingCommand = new RelayCommandAsync<int>(CancelBookingAsync);
            OpenChatCommand = new RelayCommandAsync<int>(OpenChatAsync);
            AddReviewCommand = new RelayCommandAsync<int>(AddReviewAsync);
            ProcessPaymentCommand = new RelayCommandAsync<int>(ProcessPaymentAsync);
            SearchCommand = new RelayCommandAsync(ApplyFilters);
            RefreshCommand = new RelayCommandAsync(LoadBookingsAsync);

            // Загрузка данных
            _ = LoadBookingsAsync();
        }

        private async Task LoadBookingsAsync()
        {
            try
            {
                IsBusy = true;
                ClearErrors();

                // Получаем все бронирования клиента
                var bookingsResponse = await BookingContext.GetMyBookings(_token, null, 1, 100);

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Bookings.Clear();

                    if (bookingsResponse?.Bookings != null)
                    {
                        foreach (var booking in bookingsResponse.Bookings)
                        {
                            Bookings.Add(new BookingDisplayModel
                            {
                                BookingID = booking.BookingID,
                                FriendProfileID = 0, // Не приходит в DTO, нужно добавить
                                FriendName = booking.FriendName,
                                FriendCity = booking.FriendCity,
                                Purpose = booking.Purpose,
                                MeetingLocation = booking.MeetingLocation ?? "",
                                Status = booking.Status,
                                PaymentStatus = booking.PaymentStatus,
                                TotalAmount = booking.TotalAmount,
                                Date = booking.ScheduleDate,
                                StartTime = booking.StartTime,
                                EndTime = booking.EndTime,
                                CreatedAt = booking.CreatedAt,
                                SpecialRequests = null, // Не приходит в DTO
                                HasReview = booking.HasReview,
                                HasChat = false // Нужно отдельно проверять
                            });
                        }
                    }
                });

                // Загружаем статистику
                await LoadStatisticsAsync();

                // Применяем фильтры
                ApplyFilters();
            }
            catch (Exception ex)
            {
                SetError($"Ошибка загрузки бронирований: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadStatisticsAsync()
        {
            try
            {
                var stats = await BookingContext.GetBookingStatistics(_token);

                if (stats != null)
                {
                    TotalBookings = stats.Statistics.TotalBookings;
                    ActiveBookings = stats.Statistics.ActiveBookings;
                    TotalSpent = stats.Statistics.TotalSpent;
                    AverageCheck = stats.Statistics.AverageCheck;
                }
            }
            catch (Exception ex)
            {
                SetError($"Ошибка загрузки статистики: {ex.Message}");
            }
        }

        private async Task ApplyFilter(string filter)
        {
            CurrentFilter = filter;
            await ApplyFiltersAsync();
        }

        private Task ApplyFilters()
        {
            _ = ApplyFiltersAsync();
            return Task.CompletedTask;
        }

        private async Task ApplyFiltersAsync()
        {
            await Task.Run(() =>
            {
                var filtered = Bookings.AsEnumerable();

                // Фильтр по статусу
                if (CurrentFilter != "All")
                {
                    filtered = filtered.Where(b => b.Status == CurrentFilter);
                }

                // Поиск по тексту
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    string searchLower = SearchText.ToLower();
                    filtered = filtered.Where(b =>
                        b.FriendName.ToLower().Contains(searchLower) ||
                        b.Purpose.ToLower().Contains(searchLower) ||
                        (b.MeetingLocation?.ToLower().Contains(searchLower) ?? false) ||
                        b.FriendCity.ToLower().Contains(searchLower));
                }

                // Сортировка по дате (новые сверху)
                var sorted = filtered.OrderByDescending(b => b.Date).ToList();

                // Обновляем UI
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    FilteredBookings.Clear();
                    foreach (var booking in sorted)
                    {
                        FilteredBookings.Add(booking);
                    }
                });
            });
        }

        private async Task ShowBookingDetailsAsync(int bookingId)
        {
            try
            {
                var details = await BookingContext.GetBookingDetails(_token, bookingId);

                if (details?.Booking != null)
                {
                    Base.Messenger.Default.SendData(details.Booking);
                }
                else
                {
                    SetError("Не удалось загрузить детали бронирования");
                }
            }
            catch (Exception ex)
            {
                SetError($"Ошибка загрузки деталей: {ex.Message}");
            }
        }

        private async Task CancelBookingAsync(int bookingId)
        {
            try
            {
                IsBusy = true;
                ClearErrors();

                var result = await BookingContext.CancelBooking(_token, bookingId);

                if (result != null)
                {
                    // Обновляем локальный объект
                    var booking = Bookings.FirstOrDefault(b => b.BookingID == bookingId);
                    if (booking != null)
                    {
                        booking.Status = "Cancelled";
                    }

                    Base.Messenger.Default.SendNotification($"Бронирование #{bookingId} отменено");

                    // Обновляем данные
                    await LoadBookingsAsync();
                    await LoadStatisticsAsync();
                    ApplyFilters();
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

        private async Task OpenChatAsync(int friendProfileId)
        {
            try
            {
                // Сначала получаем профиль друга по ID
                var friendProfile = await FriendProfileContext.GetFriendProfileById(friendProfileId, _token);

                if (friendProfile?.Profile != null)
                {
                    var chat = await ChatContext.GetOrCreateChat(_token, friendProfile.Profile.UserID);

                    if (chat != null)
                    {
                        Messenger.Default.SendData(new
                        {
                            chat.ChatId,
                            FriendName = friendProfile.Profile.FullName
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                SetError($"Ошибка открытия чата: {ex.Message}");
            }
        }

        private async Task AddReviewAsync(int bookingId)
        {
            Base.Messenger.Default.SendNotification($"Добавление отзыва для бронирования #{bookingId}");
            await Task.CompletedTask;
        }

        private async Task ProcessPaymentAsync(int bookingId)
        {
            try
            {
                IsBusy = true;

                var result = await BookingContext.PayBooking(_token, bookingId);

                if (result != null)
                {
                    Base.Messenger.Default.SendNotification($"Бронирование #{bookingId} оплачено");

                    // Обновляем данные
                    await LoadBookingsAsync();
                    await LoadStatisticsAsync();
                    ApplyFilters();
                }
                else
                {
                    SetError("Ошибка оплаты бронирования");
                }
            }
            catch (Exception ex)
            {
                SetError($"Ошибка оплаты: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }

    // Модель для отображения бронирования в UI
    public class BookingDisplayModel : BaseViewModel
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
            set => SetProperty(ref _status, value);
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

        // Вычисляемые свойства
        public string Duration => $"{StartTime:hh\\:mm} - {EndTime:hh\\:mm}";
        public string StatusDisplay => Status switch
        {
            "Pending" => "Ожидает подтверждения",
            "Confirmed" => "Подтверждено",
            "Completed" => "Завершено",
            "Cancelled" => "Отменено",
            "Rejected" => "Отклонено",
            _ => Status
        };

        public System.Windows.Media.Brush StatusColor => Status switch
        {
            "Pending" => System.Windows.Media.Brushes.Orange,
            "Confirmed" => System.Windows.Media.Brushes.Green,
            "Completed" => System.Windows.Media.Brushes.Blue,
            "Cancelled" => System.Windows.Media.Brushes.Red,
            "Rejected" => System.Windows.Media.Brushes.DarkRed,
            _ => System.Windows.Media.Brushes.Gray
        };

        public string PaymentStatusDisplay => PaymentStatus == "Paid" ? "Оплачено" : "Не оплачено";
        public string TotalAmountDisplay => $"{TotalAmount:N0} ₽";
        public string DateDisplay => Date.ToString("dd.MM.yyyy");
        public string TimeDisplay => $"{StartTime:hh\\:mm}";
    }
}