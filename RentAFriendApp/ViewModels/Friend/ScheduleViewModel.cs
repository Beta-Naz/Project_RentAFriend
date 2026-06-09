using RentAFriendApp.Context;
using RentAFriendApp.Models.ClassesDTO.ScheduleDTO;
using RentAFriendApp.ViewModels.Base;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace RentAFriendApp.ViewModels.Friend
{
    internal class ScheduleViewModel : BaseViewModel
    {
        private readonly string _token;
        private int _currentUserId;
        private int _profileId;
        private DateTime _currentMonth = DateTime.Today;

        // Свойства
        private DateTime _selectedDate = DateTime.Today;
        public DateTime SelectedDate
        {
            get => _selectedDate;
            set
            {
                if (SetProperty(ref _selectedDate, value))
                {
                    OnPropertyChanged(nameof(DateDisplay));
                    OnPropertyChanged(nameof(SelectedDateDisplay));
                    _ = LoadScheduleForDateAsync();
                }
            }
        }

        private TimeSpan _newStartTime = new TimeSpan(9, 0, 0);
        public TimeSpan NewStartTime
        {
            get => _newStartTime;
            set
            {
                if (SetProperty(ref _newStartTime, value))
                {
                    OnPropertyChanged(nameof(DurationDisplay));
                    OnPropertyChanged(nameof(CanAddTimeSlot));
                }
            }
        }

        private TimeSpan _newEndTime = new TimeSpan(17, 0, 0);
        public TimeSpan NewEndTime
        {
            get => _newEndTime;
            set
            {
                if (SetProperty(ref _newEndTime, value))
                {
                    OnPropertyChanged(nameof(DurationDisplay));
                    OnPropertyChanged(nameof(CanAddTimeSlot));
                }
            }
        }

        private ObservableCollection<ScheduleSlot> _scheduleSlots;
        public ObservableCollection<ScheduleSlot> ScheduleSlots
        {
            get => _scheduleSlots;
            set => SetProperty(ref _scheduleSlots, value);
        }

        private ObservableCollection<CalendarDay> _calendarDays;
        public ObservableCollection<CalendarDay> CalendarDays
        {
            get => _calendarDays;
            set => SetProperty(ref _calendarDays, value);
        }

        // Команды
        public ICommand AddTimeSlotCommand { get; }
        public ICommand RemoveTimeSlotCommand { get; }
        public ICommand ToggleAvailabilityCommand { get; }
        public ICommand NextDayCommand { get; }
        public ICommand PreviousDayCommand { get; }
        public ICommand TodayCommand { get; }
        public ICommand GenerateWeekScheduleCommand { get; }
        public ICommand SelectDateCommand { get; }
        public ICommand RefreshCommand { get; }

        public ScheduleViewModel(string token, int userId)
        {
            _token = token;
            _currentUserId = userId;
            Title = "Управление расписанием";

            ScheduleSlots = new ObservableCollection<ScheduleSlot>();
            CalendarDays = new ObservableCollection<CalendarDay>();

            AddTimeSlotCommand = new RelayCommandAsync(AddTimeSlotAsync, () => CanAddTimeSlot);
            RemoveTimeSlotCommand = new RelayCommandAsync<ScheduleSlot>(RemoveTimeSlotAsync);
            ToggleAvailabilityCommand = new RelayCommandAsync<ScheduleSlot>(ToggleAvailabilityAsync);
            NextDayCommand = new RelayCommandAsync(NextDay);
            PreviousDayCommand = new RelayCommandAsync(PreviousDay);
            TodayCommand = new RelayCommandAsync(Today);
            GenerateWeekScheduleCommand = new RelayCommandAsync(GenerateWeekScheduleAsync);
            SelectDateCommand = new RelayCommandAsync<DateTime>(SelectDate);
            RefreshCommand = new RelayCommandAsync(RefreshAsync);

            GenerateCalendarDays();
            _ = InitializeAsync();
        }

        public async Task InitializeAsync()
        {
            try
            {
                IsBusy = true;
                ClearErrors();

                await LoadProfileAsync();
                await LoadScheduleForDateAsync();
                await LoadCalendarDataAsync();

                Base.Messenger.Default.SendNotification("Расписание загружено");
            }
            catch (Exception ex)
            {
                SetError($"Ошибка инициализации: {ex.Message}");
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
                var profilesResponse = await FriendProfileContext.GetAllProfiles(_token);
                var profile = profilesResponse?.Profiles?.FirstOrDefault(p => p.UserID == _currentUserId);

                if (profile != null)
                {
                    _profileId = profile.ProfileID;

                    if (!profile.IsVerified)
                    {
                        Base.Messenger.Default.SendNotification("Ваш профиль не верифицирован");
                    }
                }
                else
                {
                    SetError("Профиль не найден");
                }
            }
            catch (Exception ex)
            {
                SetError($"Ошибка загрузки профиля: {ex.Message}");
                throw;
            }
        }

        public bool CanAddTimeSlot
        {
            get
            {
                return !IsBusy &&
                       NewEndTime > NewStartTime &&
                       (NewEndTime - NewStartTime).TotalHours <= 8 &&
                       (NewEndTime - NewStartTime).TotalHours >= 0.5;
            }
        }

        public bool HasSlots => ScheduleSlots != null && ScheduleSlots.Any();

        private async Task AddTimeSlotAsync()
        {
            try
            {
                IsBusy = true;
                ClearErrors();

                // Проверка на пересечение
                var overlapData = new CheckOverlapDTO
                {
                    ProfileID = _profileId,
                    Date = SelectedDate,
                    StartTime = NewStartTime,
                    EndTime = NewEndTime
                };

                var hasOverlap = await ScheduleContext.CheckTimeSlotOverlap(_token, overlapData);

                if (hasOverlap == true)
                {
                    SetError("Временной слот пересекается с существующим");
                    return;
                }

                // Создание слота
                var newSlotData = new CreateScheduleResponse
                {
                    Date = SelectedDate,
                    StartTime = NewStartTime,
                    EndTime = NewEndTime
                };

                var createdSlot = await ScheduleContext.CreateTimeSlot(_token, newSlotData);

                if (createdSlot != null)
                {
                    var newSlot = new ScheduleSlot
                    {
                        ScheduleID = createdSlot.ScheduleID,
                        StartTime = createdSlot.StartTime,
                        EndTime = createdSlot.EndTime,
                        IsAvailable = createdSlot.IsAvailable,
                        IsBooked = false,
                        Date = createdSlot.Date
                    };

                    ScheduleSlots.Add(newSlot);
                    SortScheduleSlots();
                    await UpdateCalendarDayAsync(SelectedDate, true);

                    NewStartTime = new TimeSpan(9, 0, 0);
                    NewEndTime = new TimeSpan(17, 0, 0);

                    Base.Messenger.Default.SendNotification($"Слот добавлен: {newSlot.TimeRange}");
                }
            }
            catch (Exception ex)
            {
                SetError($"Ошибка добавления слота: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task RemoveTimeSlotAsync(ScheduleSlot? slot)
        {
            if (slot == null) return;

            if (MessageBox.Show(
                $"Удалить слот {slot.TimeRange}?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    IsBusy = true;

                    var deleted = await ScheduleContext.DeleteTimeSlot(_token, slot.ScheduleID);

                    if (deleted)
                    {
                        ScheduleSlots.Remove(slot);
                        await UpdateCalendarDayAsync(SelectedDate, false);
                        Base.Messenger.Default.SendNotification("Слот удален");
                    }
                }
                catch (Exception ex)
                {
                    SetError($"Ошибка удаления слота: {ex.Message}");
                }
                finally
                {
                    IsBusy = false;
                }
            }
        }

        private async Task ToggleAvailabilityAsync(ScheduleSlot? slot)
        {
            if (slot != null && !slot.IsBooked)
            {
                try
                {
                    bool newAvailability = !slot.IsAvailable;

                    var result = await ScheduleContext.UpdateTimeSlotAvailability(_token, slot.ScheduleID, newAvailability);

                    if (result != null)
                    {
                        slot.IsAvailable = newAvailability;
                        await LoadScheduleForDateAsync();

                        Base.Messenger.Default.SendNotification(
                            $"Слот {slot.TimeRange} теперь {(slot.IsAvailable ? "доступен" : "недоступен")}");
                    }
                }
                catch (Exception ex)
                {
                    SetError($"Ошибка изменения доступности: {ex.Message}");
                }
            }
        }

        private async Task NextDay()
        {
            SelectedDate = SelectedDate.AddDays(1);
            await Task.CompletedTask;
        }

        private async Task PreviousDay()
        {
            SelectedDate = SelectedDate.AddDays(-1);
            await Task.CompletedTask;
        }

        private async Task Today()
        {
            SelectedDate = DateTime.Today;
            await Task.CompletedTask;
        }

        private async Task SelectDate(DateTime date)
        {
            SelectedDate = date;
            await Task.CompletedTask;
        }

        private async Task GenerateWeekScheduleAsync()
        {
            try
            {
                IsBusy = true;
                ClearErrors();

                if (MessageBox.Show(
                    "Сгенерировать стандартное расписание на неделю? Существующие слоты будут удалены.",
                    "Подтверждение",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question) != MessageBoxResult.Yes)
                {
                    return;
                }

                var result = await ScheduleContext.CreateDefaultWeekSchedule(_token, DateTime.Today);

                if (result != null)
                {
                    await LoadScheduleForDateAsync();
                    await LoadCalendarDataAsync();

                    Base.Messenger.Default.SendNotification($"Расписание на неделю создано ({result.SlotsCount} слотов)");
                }
            }
            catch (Exception ex)
            {
                SetError($"Ошибка генерации расписания: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadScheduleForDateAsync()
        {
            try
            {
                var schedule = await ScheduleContext.GetScheduleByDate(_profileId, SelectedDate, _token);

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    ScheduleSlots.Clear();

                    if (schedule?.Slots != null)
                    {
                        foreach (var slot in schedule.Slots)
                        {
                            ScheduleSlots.Add(new ScheduleSlot
                            {
                                ScheduleID = slot.ScheduleID,
                                StartTime = slot.StartTime,
                                EndTime = slot.EndTime,
                                IsAvailable = slot.IsAvailable,
                                IsBooked = slot.IsBooked,
                                Date = SelectedDate
                            });
                        }
                    }

                    SortScheduleSlots();
                    OnPropertyChanged(nameof(HasScheduleSlots));
                    OnPropertyChanged(nameof(TotalSlots));
                    OnPropertyChanged(nameof(AvailableSlots));
                    OnPropertyChanged(nameof(BookedSlots));
                });
            }
            catch (Exception ex)
            {
                SetError($"Ошибка загрузки расписания: {ex.Message}");
            }
        }

        private void SortScheduleSlots()
        {
            var sorted = ScheduleSlots.OrderBy(s => s.StartTime).ToList();
            ScheduleSlots = new ObservableCollection<ScheduleSlot>(sorted);
        }

        private async Task LoadCalendarDataAsync()
        {
            try
            {
                var startDate = new DateTime(_currentMonth.Year, _currentMonth.Month, 1);
                var endDate = startDate.AddMonths(1).AddDays(-1);

                var calendarStats = await ScheduleContext.GetCalendarStats(_token, _profileId, startDate, endDate);

                var dateStats = calendarStats?.ToDictionary(s => s.Date, s => s.SlotCount) ?? new Dictionary<DateTime, int>();

                foreach (var day in CalendarDays.Where(d => d.Day > 0))
                {
                    if (dateStats.ContainsKey(day.Date))
                    {
                        day.HasSlots = true;
                        day.SlotCount = dateStats[day.Date];
                    }
                    else
                    {
                        day.HasSlots = false;
                        day.SlotCount = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки календаря: {ex.Message}");
            }
        }

        private async Task UpdateCalendarDayAsync(DateTime date, bool increment)
        {
            var day = CalendarDays.FirstOrDefault(d => d.Date.Date == date.Date);
            if (day != null)
            {
                day.SlotCount += increment ? 1 : -1;
                day.HasSlots = day.SlotCount > 0;
            }
            await Task.CompletedTask;
        }

        private void GenerateCalendarDays()
        {
            CalendarDays.Clear();

            var today = DateTime.Today;
            var firstDayOfMonth = new DateTime(_currentMonth.Year, _currentMonth.Month, 1);
            var daysInMonth = DateTime.DaysInMonth(_currentMonth.Year, _currentMonth.Month);

            int firstDayOfWeek = (int)firstDayOfMonth.DayOfWeek;
            firstDayOfWeek = firstDayOfWeek == 0 ? 6 : firstDayOfWeek - 1;

            for (int i = 0; i < firstDayOfWeek; i++)
            {
                CalendarDays.Add(new CalendarDay { Day = 0 });
            }

            for (int day = 1; day <= daysInMonth; day++)
            {
                var date = new DateTime(_currentMonth.Year, _currentMonth.Month, day);
                CalendarDays.Add(new CalendarDay
                {
                    Day = day,
                    Date = date,
                    IsToday = date.Date == today.Date,
                    IsSelected = date.Date == SelectedDate.Date,
                    HasSlots = false,
                    SlotCount = 0
                });
            }
        }

        public void UpdateCalendarMonth(DateTime month)
        {
            _currentMonth = month;
            GenerateCalendarDays();
            _ = LoadCalendarDataAsync();
            OnPropertyChanged(nameof(CurrentMonthYear));
        }

        public async Task RefreshAsync()
        {
            await LoadScheduleForDateAsync();
            await LoadCalendarDataAsync();
            Base.Messenger.Default.SendNotification("Данные обновлены");
        }

        // Вычисляемые свойства
        public string DateDisplay => SelectedDate.ToString("dddd, dd MMMM yyyy");
        public string SelectedDateDisplay => SelectedDate.ToString("dd MMMM yyyy");
        public string CurrentMonthYear => _currentMonth.ToString("MMMM yyyy");
        public int TotalSlots => ScheduleSlots.Count;
        public int AvailableSlots => ScheduleSlots.Count(s => s.IsAvailable && !s.IsBooked);
        public int BookedSlots => ScheduleSlots.Count(s => s.IsBooked);
        public string DurationDisplay =>
            $"{NewStartTime:hh\\:mm} - {NewEndTime:hh\\:mm} ({(NewEndTime - NewStartTime).TotalHours:0.##} ч)";
        public bool HasScheduleSlots => ScheduleSlots.Any();

        // Внутренний класс для временных слотов
        internal class ScheduleSlot : BaseViewModel
        {
            public int ScheduleID { get; set; }
            public TimeSpan StartTime { get; set; }
            public TimeSpan EndTime { get; set; }
            private bool _isAvailable;
            public bool IsAvailable
            {
                get => _isAvailable;
                set => SetProperty(ref _isAvailable, value);
            }
            public bool IsBooked { get; set; }
            public DateTime Date { get; set; }
            public string? BookingStatus { get; set; }
            public string? BookingPurpose { get; set; }

            public Brush StatusColor
            {
                get
                {
                    if (IsBooked) return new SolidColorBrush(Color.FromRgb(211, 47, 47));
                    return IsAvailable ?
                        new SolidColorBrush(Color.FromRgb(76, 175, 80)) :
                        new SolidColorBrush(Color.FromRgb(158, 158, 158));
                }
            }

            public string TimeRange => $"{StartTime:hh\\:mm} - {EndTime:hh\\:mm}";
            public TimeSpan Duration => EndTime - StartTime;
            public string DurationDisplay => $"{(int)Duration.TotalHours} ч {Duration.Minutes} мин";

            public string Status
            {
                get
                {
                    if (IsBooked) return "Забронировано";
                    return IsAvailable ? "Доступно" : "Недоступно";
                }
            }
        }

        // Внутренний класс для дней календаря
        internal class CalendarDay : BaseViewModel
        {
            public int Day { get; set; }
            public DateTime Date { get; set; }
            public bool IsToday { get; set; }
            public bool IsSelected { get; set; }
            private bool _hasSlots;
            public bool HasSlots
            {
                get => _hasSlots;
                set => SetProperty(ref _hasSlots, value);
            }
            private int _slotCount;
            public int SlotCount
            {
                get => _slotCount;
                set => SetProperty(ref _slotCount, value);
            }
        }
    }
}