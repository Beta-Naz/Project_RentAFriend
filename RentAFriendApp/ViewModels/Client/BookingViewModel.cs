using RentAFriendApp.Context;
using RentAFriendApp.Models.ClassesDTO.BookingDTO;
using RentAFriendApp.Models.ClassesDTO.FriendProfileDTO;
using RentAFriendApp.Models.ClassesDTO.FriendProfileDTO.Response;
using RentAFriendApp.Models.ClassesDTO.ScheduleDTO;
using RentAFriendApp.ViewModels.Base;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace RentAFriendApp.ViewModels.Client
{
    internal class BookingViewModel : BaseViewModel
    {
        private readonly string _token;
        private FPInfoDTO _selectedFriend;

        // Данные для бронирования
        private DateTime _selectedDate = DateTime.Today;
        public DateTime SelectedDate
        {
            get => _selectedDate;
            set
            {
                if (SetProperty(ref _selectedDate, value))
                {
                    _ = LoadAvailableTimeSlotsAsync();
                }
            }
        }

        private TimeSpan _selectedStartTime;
        public TimeSpan SelectedStartTime
        {
            get => _selectedStartTime;
            set
            {
                if (SetProperty(ref _selectedStartTime, value))
                {
                    UpdateDuration();
                    CalculateTotalAmount();
                }
            }
        }

        private TimeSpan _selectedEndTime;
        public TimeSpan SelectedEndTime
        {
            get => _selectedEndTime;
            set
            {
                if (SetProperty(ref _selectedEndTime, value))
                {
                    UpdateDuration();
                    CalculateTotalAmount();
                }
            }
        }

        private string _purpose = string.Empty;
        public string Purpose
        {
            get => _purpose;
            set => SetProperty(ref _purpose, value);
        }

        private string _meetingLocation = string.Empty;
        public string MeetingLocation
        {
            get => _meetingLocation;
            set => SetProperty(ref _meetingLocation, value);
        }

        private string _specialRequests = string.Empty;
        public string SpecialRequests
        {
            get => _specialRequests;
            set => SetProperty(ref _specialRequests, value);
        }

        // Вычисляемые свойства
        private TimeSpan _duration;
        public TimeSpan Duration
        {
            get => _duration;
            set => SetProperty(ref _duration, value);
        }

        private decimal _totalAmount;
        public decimal TotalAmount
        {
            get => _totalAmount;
            set => SetProperty(ref _totalAmount, value);
        }

        // Доступные временные слоты
        private ObservableCollection<TimeSlot> _availableTimeSlots;
        public ObservableCollection<TimeSlot> AvailableTimeSlots
        {
            get => _availableTimeSlots;
            set => SetProperty(ref _availableTimeSlots, value);
        }

        // Команды
        public ICommand CreateBookingCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand SelectTimeSlotCommand { get; }

        public BookingViewModel(string token, FPInfoDTO friend)
        {
            _token = token;
            _selectedFriend = friend;
            Title = $"Бронирование: {friend?.FullName ?? "Друг"}";

            AvailableTimeSlots = new ObservableCollection<TimeSlot>();

            // Инициализация команд
            CreateBookingCommand = new RelayCommandAsync(CreateBookingAsync, CanCreateBooking);
            CancelCommand = new RelayCommandAsync(Cancel);
            SelectTimeSlotCommand = new RelayCommandAsync<TimeSlot>(SelectTimeSlotAsync);

            // Установка времени по умолчанию
            SelectedStartTime = new TimeSpan(14, 0, 0); // 14:00
            SelectedEndTime = new TimeSpan(15, 0, 0);   // 15:00

            // Загрузка доступных слотов
            _ = LoadAvailableTimeSlotsAsync();
        }

        private bool CanCreateBooking()
        {
            return !IsBusy &&
                   _selectedFriend != null &&
                   !string.IsNullOrWhiteSpace(Purpose) &&
                   SelectedStartTime < SelectedEndTime &&
                   Duration.TotalHours >= 1 &&
                   Duration.TotalHours <= 8;
        }

        private async Task CreateBookingAsync()
        {
            try
            {
                IsBusy = true;
                ClearErrors();

                // Валидация
                if (!ValidateBooking())
                    return;

                // Проверяем доступность времени
                var scheduleId = await FindAvailableScheduleIdAsync();
                if (!scheduleId.HasValue)
                {
                    SetError("Выбранное время больше недоступно. Пожалуйста, выберите другое время.");
                    return;
                }

                // Создаем бронирование
                var bookingData = new CreateBookingDTO
                {
                    ScheduleID = scheduleId.Value,
                    Purpose = Purpose,
                    MeetingLocation = MeetingLocation,
                    SpecialRequests = SpecialRequests
                };

                var result = await BookingContext.CreateBooking(_token, bookingData);

                if (result != null)
                {
                    Base.Messenger.Default.SendNotification($"Бронирование создано! ID: {result.BookingId}");
                    await Cancel();
                }
                else
                {
                    SetError("Ошибка создания бронирования");
                }
            }
            catch (Exception ex)
            {
                SetError($"Ошибка создания бронирования: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task Cancel()
        {
            Base.Messenger.Default.SendNotification("Отмена бронирования");
            await Task.CompletedTask;
        }

        private async Task SelectTimeSlotAsync(TimeSlot timeSlot)
        {
            if (timeSlot != null)
            {
                SelectedStartTime = timeSlot.StartTime;
                SelectedEndTime = timeSlot.EndTime;
            }
            await Task.CompletedTask;
        }

        private async Task LoadAvailableTimeSlotsAsync()
        {
            try
            {
                IsBusy = true;
                AvailableTimeSlots.Clear();

                if (_selectedFriend == null)
                    return;

                // Получаем доступные слоты через ScheduleContext
                var availableSlots = await ScheduleContext.GetAvailableTimeSlots(
                    _selectedFriend.ProfileID, SelectedDate, _token);

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (availableSlots?.Slots != null)
                    {
                        foreach (var slot in availableSlots.Slots)
                        {
                            AvailableTimeSlots.Add(new TimeSlot
                            {
                                ScheduleID = slot.ScheduleID,
                                StartTime = slot.StartTime,
                                EndTime = slot.EndTime
                            });
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                SetError($"Ошибка загрузки доступного времени: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void UpdateDuration()
        {
            Duration = SelectedEndTime - SelectedStartTime;
            OnPropertyChanged(nameof(DurationDisplay));
        }

        private void CalculateTotalAmount()
        {
            if (_selectedFriend?.HourlyRate.HasValue == true)
            {
                TotalAmount = _selectedFriend.HourlyRate.Value * (decimal)Duration.TotalHours;
            }
            else
            {
                TotalAmount = 0;
            }
        }

        private bool ValidateBooking()
        {
            if (string.IsNullOrWhiteSpace(Purpose))
            {
                SetError("Укажите цель встречи");
                return false;
            }

            if (Duration.TotalHours < 1)
            {
                SetError("Минимальная продолжительность - 1 час");
                return false;
            }

            if (Duration.TotalHours > 8)
            {
                SetError("Максимальная продолжительность - 8 часов");
                return false;
            }

            if (SelectedDate < DateTime.Today)
            {
                SetError("Нельзя бронировать на прошедшую дату");
                return false;
            }

            return true;
        }

        private async Task<int?> FindAvailableScheduleIdAsync()
        {
            try
            {
                // Получаем доступные слоты на выбранную дату
                var availableSlots = await ScheduleContext.GetAvailableTimeSlots(
                    _selectedFriend.ProfileID, SelectedDate, _token);

                if (availableSlots?.Slots == null)
                    return null;

                // Ищем слот, который соответствует выбранному времени
                var matchingSlot = availableSlots.Slots.FirstOrDefault(s =>
                    s.StartTime == SelectedStartTime && s.EndTime == SelectedEndTime);

                return matchingSlot?.ScheduleID;
            }
            catch (Exception ex)
            {
                SetError($"Ошибка проверки доступности времени: {ex.Message}");
                return null;
            }
        }

        // Вспомогательный класс для временных слотов
        public class TimeSlot
        {
            public int ScheduleID { get; set; }
            public TimeSpan StartTime { get; set; }
            public TimeSpan EndTime { get; set; }

            public string Display => $"{StartTime:hh\\:mm} - {EndTime:hh\\:mm}";
            public TimeSpan Duration => EndTime - StartTime;
        }

        public string DurationDisplay
        {
            get
            {
                var hours = (int)Duration.TotalHours;
                var minutes = Duration.Minutes;

                if (hours > 0 && minutes > 0)
                    return $"{hours} ч {minutes} мин";
                if (hours > 0)
                    return $"{hours} ч";
                return $"{minutes} мин";
            }
        }

        public string FriendName => _selectedFriend?.FullName ?? "Неизвестный";
        public string FriendRateDisplay => _selectedFriend?.HourlyRate.HasValue == true
            ? $"{_selectedFriend.HourlyRate.Value:C}/час"
            : "Не указано";
    }
}