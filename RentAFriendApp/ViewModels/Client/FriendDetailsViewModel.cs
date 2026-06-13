using RentAFriendApp.Context;
using RentAFriendApp.Models.ClassesDTO.FriendProfileDTO;
using RentAFriendApp.Models.ClassesDTO.ReviewDTO;
using RentAFriendApp.ViewModels.Base;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RentAFriendApp.ViewModels.Client
{
    internal class FriendDetailsViewModel : BaseViewModel
    {
        private readonly string _token;
        private FPInfoDTO _currentFriend;
        public FPInfoDTO CurrentFriend => _currentFriend;
        private DateTime _selectedDate = DateTime.Today;

        // Отзывы
        private ObservableCollection<ReviewDTO> _reviews;
        public ObservableCollection<ReviewDTO> Reviews
        {
            get => _reviews;
            set => SetProperty(ref _reviews, value);
        }

        // Доступные временные слоты
        private ObservableCollection<ScheduleSlot> _availableTimeSlots;
        public ObservableCollection<ScheduleSlot> AvailableTimeSlots
        {
            get => _availableTimeSlots;
            set => SetProperty(ref _availableTimeSlots, value);
        }

        // Выбранный временной слот
        private ScheduleSlot _selectedTimeSlot;
        public ScheduleSlot SelectedTimeSlot
        {
            get => _selectedTimeSlot;
            set
            {
                if (SetProperty(ref _selectedTimeSlot, value))
                {
                    CalculateTotalAmount();
                    OnPropertyChanged(nameof(HasSelectedTimeSlot));
                    OnPropertyChanged(nameof(SelectedTimeDisplay));
                    OnPropertyChanged(nameof(SelectedDuration));
                }
            }
        }

        // Ближайшие даты для выбора
        private ObservableCollection<DateTime> _availableDates;
        public ObservableCollection<DateTime> AvailableDates
        {
            get => _availableDates;
            set => SetProperty(ref _availableDates, value);
        }

        // Выбранная дата
        public DateTime SelectedDate
        {
            get => _selectedDate;
            set
            {
                if (SetProperty(ref _selectedDate, value))
                {
                    UpdateSelectedDateDisplay();
                    _ = LoadAvailableTimeSlotsForDateAsync();
                }
            }
        }

        // ========== ДОБАВЛЕННЫЕ СВОЙСТВА ДЛЯ XAML ==========

        private string _friendInitials;
        public string FriendInitials
        {
            get => _friendInitials;
            set => SetProperty(ref _friendInitials, value);
        }

        private string _friendRatingDisplay;
        public string FriendRatingDisplay
        {
            get => _friendRatingDisplay;
            set => SetProperty(ref _friendRatingDisplay, value);
        }

        private string _reviewCountDisplay;
        public string ReviewCountDisplay
        {
            get => _reviewCountDisplay;
            set => SetProperty(ref _reviewCountDisplay, value);
        }

        private int _totalBookings;
        public int TotalBookings
        {
            get => _totalBookings;
            set => SetProperty(ref _totalBookings, value);
        }

        private string _completedPercent;
        public string CompletedPercent
        {
            get => _completedPercent;
            set => SetProperty(ref _completedPercent, value);
        }

        private int _reviewCount;
        public int ReviewCount
        {
            get => _reviewCount;
            set => SetProperty(ref _reviewCount, value);
        }

        private string _reviewHeader;
        public string ReviewHeader
        {
            get => _reviewHeader;
            set => SetProperty(ref _reviewHeader, value);
        }

        private string _selectedDateDisplay;
        public string SelectedDateDisplay
        {
            get => _selectedDateDisplay;
            set => SetProperty(ref _selectedDateDisplay, value);
        }

        // Итоговая стоимость
        private decimal _totalAmount;
        public decimal TotalAmount
        {
            get => _totalAmount;
            set => SetProperty(ref _totalAmount, value);
        }

        // Команды
        public ICommand BackCommand { get; }
        public ICommand BookCommand { get; }
        public ICommand MessageCommand { get; }
        public ICommand ShareCommand { get; }
        public ICommand ViewAllReviewsCommand { get; }
        public ICommand RefreshCommand { get; }

        // Вычисляемые свойства
        public string FriendName => _currentFriend?.FullName ?? "Неизвестный";
        public string FriendCity => _currentFriend?.City ?? "Не указан";
        public decimal? FriendRating => _currentFriend?.AverageRating;
        public string FriendBio => _currentFriend?.Bio ?? "Нет описания";
        public string FriendHobbies => _currentFriend?.Hobbies ?? "Не указаны";
        public string FriendRateDisplay => _currentFriend?.HourlyRate.HasValue == true
            ? $"{_currentFriend.HourlyRate.Value:N0} ₽/час"
            : "Цена не указана";
        public bool IsVerified => _currentFriend?.IsVerified == true;
        public bool HasSelectedTimeSlot => SelectedTimeSlot != null;
        public string SelectedTimeDisplay => SelectedTimeSlot != null
            ? $"{SelectedTimeSlot.StartTime:hh\\:mm} - {SelectedTimeSlot.EndTime:hh\\:mm}"
            : "Не выбрано";
        public TimeSpan SelectedDuration => SelectedTimeSlot != null
            ? SelectedTimeSlot.EndTime - SelectedTimeSlot.StartTime
            : TimeSpan.Zero;

        public FriendDetailsViewModel(string token, FPInfoDTO friend)
        {
            _token = token;
            _currentFriend = friend;
            Title = $"Профиль: {friend?.FullName ?? "Друг"}";

            // Инициализация коллекций
            Reviews = new ObservableCollection<ReviewDTO>();
            AvailableTimeSlots = new ObservableCollection<ScheduleSlot>();
            AvailableDates = new ObservableCollection<DateTime>();

            // Инициализация свойств из friend
            InitializePropertiesFromFriend();

            // Инициализация команд
            BackCommand = new RelayCommandAsync(GoBack);
            BookCommand = new RelayCommandAsync(BookMeetingAsync, CanBookMeeting);
            MessageCommand = new RelayCommandAsync(SendMessageAsync);
            ShareCommand = new RelayCommandAsync(ShareProfileAsync);
            ViewAllReviewsCommand = new RelayCommandAsync(ViewAllReviewsAsync);
            RefreshCommand = new RelayCommandAsync(LoadDataAsync);

            // Загрузка данных
            _ = LoadDataAsync();
        }

        private async Task InitializePropertiesFromFriend()
        {
            if (_currentFriend == null) return;

            // Инициализация инициалов
            FriendInitials = GetInitials(_currentFriend.FullName);

            // Инициализация рейтинга
            if (_currentFriend.AverageRating.HasValue)
            {
                FriendRatingDisplay = _currentFriend.AverageRating.Value.ToString("0.0");
            }
            else
            {
                FriendRatingDisplay = "Нет оценок";
            }
            var profileStats = await FriendProfileContext.GetFriendProfileStats(_token, _currentFriend.ProfileID);
            var stats = profileStats.Statistic;
            // Инициализация количества отзывов
            ReviewCount = stats?.ReviewCount ?? 0;
            UpdateReviewDisplayProperties();

            // Инициализация статистики
            TotalBookings = stats?.CompletedBookings ?? 0;
            float? completedPercent = (float?)(stats?.TotalBookings / 100) * stats?.CompletedBookings;
            CompletedPercent = $"{completedPercent ?? 0}%";
            if(stats?.TotalBookings == 0)
            {
                CompletedPercent = "None";
            }
            // Инициализация отображения даты
            UpdateSelectedDateDisplay();
        }

        private void UpdateReviewDisplayProperties()
        {
            ReviewCountDisplay = $"({ReviewCount} {GetReviewWord(ReviewCount)})";
            ReviewHeader = $"Отзывы ({ReviewCount})";
        }

        private void UpdateSelectedDateDisplay()
        {
            SelectedDateDisplay = $"Выбрана дата: {SelectedDate:dd.MM.yyyy}";
        }

        private string GetInitials(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return "?";

            var parts = fullName.Trim().Split(' ');
            if (parts.Length >= 2 && parts[0].Length > 0 && parts[1].Length > 0)
                return $"{parts[0][0]}{parts[1][0]}".ToUpper();

            if (parts.Length >= 1 && parts[0].Length > 0)
                return parts[0][0].ToString().ToUpper();

            return "?";
        }

        private string GetReviewWord(int count)
        {
            if (count % 10 == 1 && count % 100 != 11)
                return "отзыв";
            if (count % 10 >= 2 && count % 10 <= 4 && (count % 100 < 10 || count % 100 >= 20))
                return "отзыва";
            return "отзывов";
        }

        private async Task LoadDataAsync()
        {
            try
            {
                IsBusy = true;
                ClearErrors();

                await LoadReviewsAsync();
                LoadAvailableDates();
                await LoadAvailableTimeSlotsForDateAsync();
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

        private bool CanBookMeeting()
        {
            return !IsBusy &&
                   SelectedTimeSlot != null &&
                   _currentFriend != null &&
                   SelectedTimeSlot.IsAvailable;
        }

        private async Task BookMeetingAsync()
        {
            try
            {
                IsBusy = true;

                if (SelectedTimeSlot == null || _currentFriend == null)
                {
                    SetError("Пожалуйста, выберите время для встречи");
                    return;
                }

                if (!SelectedTimeSlot.IsAvailable)
                {
                    SetError("Выбранное время уже занято");
                    return;
                }

                var bookingData = new
                {
                    FriendProfileID = _currentFriend.ProfileID,
                    SelectedTimeSlot.ScheduleID,
                    FriendName,
                    Date = SelectedDate,
                    SelectedTimeSlot.StartTime,
                    SelectedTimeSlot.EndTime,
                    _currentFriend.HourlyRate,
                    TotalAmount
                };

                Messenger.Default.SendData(bookingData);
            }
            catch (Exception ex)
            {
                SetError($"Ошибка при создании бронирования: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task SendMessageAsync()
        {
            try
            {
                IsBusy = true;

                var chat = await ChatContext.GetOrCreateChat(_token, _currentFriend.UserID);

                if (chat != null)
                {
                    Messenger.Default.SendData(new
                    {
                        chat.ChatId,
                        FriendName,
                        FriendId = _currentFriend.UserID
                    });
                }
            }
            catch (Exception ex)
            {
                SetError($"Ошибка открытия чата: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ShareProfileAsync()
        {
            Messenger.Default.SendNotification($"Поделиться профилем {FriendName}");
            await Task.CompletedTask;
        }

        private async Task ViewAllReviewsAsync()
        {
            Messenger.Default.SendData(new
            {
                FriendProfileID = _currentFriend?.ProfileID,
                FriendName = FriendName
            });
            await Task.CompletedTask;
        }

        private async Task GoBack()
        {
            Messenger.Default.SendNotification("Возврат в каталог");
            await Task.CompletedTask;
        }

        public async Task LoadReviewsAsync()
        {
            try
            {
                if (_currentFriend == null) return;

                var reviewsResponse = await ReviewContext.GetReviewsByFriend(
                    _currentFriend.ProfileID, _token, page: 1, pageSize: 10, onlyApproved: true);

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Reviews.Clear();

                    if (reviewsResponse?.Reviews != null)
                    {
                        foreach (var review in reviewsResponse.Reviews)
                        {
                            Reviews.Add(review);
                        }
                    }

                    // Обновляем количество отзывов из загруженных данных
                    ReviewCount = Reviews.Count;
                    UpdateReviewDisplayProperties();
                });
            }
            catch (Exception ex)
            {
                SetError($"Ошибка загрузки отзывов: {ex.Message}");
            }
        }

        private void LoadAvailableDates()
        {
            try
            {
                AvailableDates.Clear();

                // Добавляем ближайшие 14 дней
                for (int i = 0; i < 14; i++)
                {
                    var date = DateTime.Today.AddDays(i);
                    AvailableDates.Add(date);
                }

                // Если SelectedDate не в списке, устанавливаем первый день
                if (!AvailableDates.Contains(SelectedDate))
                {
                    SelectedDate = AvailableDates.FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                SetError($"Ошибка загрузки дат: {ex.Message}");
            }
        }

        private async Task LoadAvailableTimeSlotsForDateAsync()
        {
            try
            {
                if (_currentFriend == null) return;

                AvailableTimeSlots.Clear();

                var availableSlots = await ScheduleContext.GetAvailableTimeSlots(
                    _currentFriend.ProfileID, SelectedDate, _token);

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (availableSlots?.Slots != null)
                    {
                        foreach (var slot in availableSlots.Slots)
                        {
                            AvailableTimeSlots.Add(new ScheduleSlot
                            {
                                ScheduleID = slot.ScheduleID,
                                StartTime = slot.StartTime,
                                EndTime = slot.EndTime,
                                IsAvailable = true,
                                IsBooked = false
                            });
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                SetError($"Ошибка загрузки расписания: {ex.Message}");
            }
        }

        private void CalculateTotalAmount()
        {
            if (_currentFriend?.HourlyRate.HasValue == true && SelectedTimeSlot != null)
            {
                var hours = (decimal)SelectedDuration.TotalHours;
                TotalAmount = _currentFriend.HourlyRate.Value * hours;
            }
            else
            {
                TotalAmount = 0;
            }
        }

        // Вспомогательный класс для временных слотов
        public class ScheduleSlot
        {
            public int ScheduleID { get; set; }
            public TimeSpan StartTime { get; set; }
            public TimeSpan EndTime { get; set; }
            public bool IsBooked { get; set; }
            public bool IsAvailable { get; set; }

            public string Display => $"{StartTime:hh\\:mm} - {EndTime:hh\\:mm}";
            public TimeSpan Duration => EndTime - StartTime;
        }
    }
}