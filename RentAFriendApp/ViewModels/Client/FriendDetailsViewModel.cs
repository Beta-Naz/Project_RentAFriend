using RentAFriendApp.Context;
using RentAFriendApp.Models.ClassesDTO.FriendProfileDTO;
using RentAFriendApp.Models.ClassesDTO.ReviewDTO;
using RentAFriendApp.ViewModels.Base;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace RentAFriendApp.ViewModels.Client
{
    internal class FriendDetailsViewModel : BaseViewModel
    {
        private readonly string _token;
        private FPInfoDTO _currentFriend;
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
                    _ = LoadAvailableTimeSlotsForDateAsync();
                }
            }
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
        public ICommand AddToFavoritesCommand { get; }
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

            // Инициализация команд
            BackCommand = new RelayCommandAsync(GoBack);
            BookCommand = new RelayCommandAsync(BookMeetingAsync, CanBookMeeting);
            MessageCommand = new RelayCommandAsync(SendMessageAsync);
            AddToFavoritesCommand = new RelayCommandAsync(AddToFavoritesAsync);
            ShareCommand = new RelayCommandAsync(ShareProfileAsync);
            ViewAllReviewsCommand = new RelayCommandAsync(ViewAllReviewsAsync);
            RefreshCommand = new RelayCommandAsync(LoadDataAsync);

            // Загрузка данных
            _ = LoadDataAsync();
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

                // Создаем или получаем чат
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

        private async Task AddToFavoritesAsync()
        {
            try
            {
                IsBusy = true;

                // TODO: Добавить метод AddToFavorites в FriendProfileContext
                // var result = await FriendProfileContext.AddToFavorites(_token, _currentFriend.ProfileID);

                Base.Messenger.Default.SendNotification($"{FriendName} добавлен в избранное");
            }
            catch (Exception ex)
            {
                SetError($"Ошибка при добавлении в избранное: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ShareProfileAsync()
        {
            // Логика для шаринга профиля
            Base.Messenger.Default.SendNotification($"Поделиться профилем {FriendName}");
            await Task.CompletedTask;
        }

        private async Task ViewAllReviewsAsync()
        {
            // Переход ко всем отзывам
            Base.Messenger.Default.SendData(new
            {
                FriendProfileID = _currentFriend?.ProfileID,
                FriendName = FriendName
            });
            await Task.CompletedTask;
        }

        private async Task GoBack()
        {
            Base.Messenger.Default.SendNotification("Возврат в каталог");
            await Task.CompletedTask;
        }

        private async Task LoadReviewsAsync()
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