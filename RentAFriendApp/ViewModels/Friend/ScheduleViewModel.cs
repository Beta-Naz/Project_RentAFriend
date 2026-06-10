using RentAFriendApp.Context;
using RentAFriendApp.Models.ClassesDTO.ScheduleDTO;
using RentAFriendApp.ViewModels.Base;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RentAFriendApp.ViewModels.Friend
{
    internal class ScheduleViewModel : BaseViewModel
    {
        private readonly string _token;
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
        private string _newStartTimeText = "09:00";
        public string NewStartTimeText
        {
            get => _newStartTimeText;
            set
            {
                if (SetProperty(ref _newStartTimeText, value))
                {
                    if (TimeSpan.TryParse(value, out TimeSpan result))
                    {
                        NewStartTime = result;
                    }
                    else
                    {
                        _newStartTimeText = NewStartTime.ToString(@"hh\:mm");
                    }
                }
            }
        }

        private string _newEndTimeText = "17:00";
        public string NewEndTimeText
        {
            get => _newEndTimeText;
            set
            {
                if (SetProperty(ref _newEndTimeText, value))
                {
                    if (TimeSpan.TryParse(value, out TimeSpan result))
                    {
                        NewEndTime = result;
                    }
                    else
                    {
                        _newEndTimeText = NewEndTime.ToString(@"hh\:mm");
                    }
                }
            }
        }
        private TimeSpan _newStartTime = new(9, 0, 0);
        public TimeSpan NewStartTime
        {
            get => _newStartTime;
            set
            {
                if (SetProperty(ref _newStartTime, value))
                {
                    NewStartTimeText = value.ToString(@"hh\:mm");
                    OnPropertyChanged(nameof(DurationDisplay));
                    OnPropertyChanged(nameof(CanAddTimeSlot));
                }
            }
        }

        private TimeSpan _newEndTime = new(17, 0, 0);
        public TimeSpan NewEndTime
        {
            get => _newEndTime;
            set
            {
                if (SetProperty(ref _newEndTime, value))
                {
                    NewEndTimeText = value.ToString(@"hh\:mm");
                    OnPropertyChanged(nameof(DurationDisplay));
                    OnPropertyChanged(nameof(CanAddTimeSlot));
                }
            }
        }

        private ObservableCollection<ScheduleSlot>? _scheduleSlots;
        public ObservableCollection<ScheduleSlot>? ScheduleSlots
        {
            get => _scheduleSlots;
            set => SetProperty(ref _scheduleSlots, value);
        }

        private ObservableCollection<CalendarDay>? _calendarDays;
        public ObservableCollection<CalendarDay>? CalendarDays
        {
            get => _calendarDays;
            set => SetProperty(ref _calendarDays, value);
        }

        // Команды
        public ICommand AddTimeSlotCommand { get; }

        public ICommand NextDayCommand { get; }
        public ICommand PreviousDayCommand { get; }
        public ICommand TodayCommand { get; }
        public ICommand GenerateWeekScheduleCommand { get; }
        public ICommand SelectDateCommand { get; }
        public ICommand RefreshCommand { get; }
        
        public ScheduleViewModel(string token)
        {
            _token = token;
            Title = "Управление расписанием";

            ScheduleSlots = [];
            CalendarDays = [];

            AddTimeSlotCommand = new RelayCommandAsync(AddTimeSlotAsync, () => CanAddTimeSlot);
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
                var user = await UserContext.GetUser(_token);
                var profilesResponse = await FriendProfileContext.GetAllProfiles(_token);
                var profile = profilesResponse?.Profiles?.FirstOrDefault(p => p.UserID == user?.UserID);

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
                       (NewEndTime - NewStartTime).TotalHours >= 0.5 && SelectedDate >= DateTime.Today;
            }
        }

        public bool HasSlots => ScheduleSlots != null && ScheduleSlots.Any();
        
        public async Task AddTimeSlotAsync()
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
                    var newSlot = new ScheduleSlot(this)
                    {
                        ScheduleID = createdSlot.ScheduleID,
                        StartTime = createdSlot.StartTime,
                        EndTime = createdSlot.EndTime,
                        IsAvailable = createdSlot.IsAvailable,
                        IsBooked = false,
                        Date = createdSlot.Date
                    };

                    ScheduleSlots?.Add(newSlot);
                    SortScheduleSlots();
                    await UpdateCalendarDayAsync(SelectedDate, true);

                    NewStartTime = new TimeSpan(9, 0, 0);
                    NewEndTime = new TimeSpan(17, 0, 0);

                    Messenger.Default.SendNotification($"Слот добавлен: {newSlot.TimeRange}");
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

        public async Task RemoveTimeSlotAsync(ScheduleSlot? slot)
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
                        ScheduleSlots?.Remove(slot);
                        await UpdateCalendarDayAsync(SelectedDate, false);
                        Messenger.Default.SendNotification("Слот удален");
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

        public async Task ToggleAvailabilityAsync(ScheduleSlot? slot)
        {
            if (slot == null || slot.IsBooked) return;

            try
            {
                IsBusy = true;
                bool newAvailability = !slot.IsAvailable;

                var result = await ScheduleContext.UpdateTimeSlotAvailability(
                    _token, slot.ScheduleID, newAvailability);

                if (result != null)
                {
                    slot.IsAvailable = newAvailability;
                    slot.UpdateToggleIcon();

                    await LoadScheduleForDateAsync();

                    Messenger.Default.SendNotification(
                        $"Слот {slot.TimeRange} теперь {(slot.IsAvailable ? "доступен" : "недоступен")}");
                }
            }
            catch (Exception ex)
            {
                SetError($"Ошибка изменения доступности: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
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

                    Messenger.Default.SendNotification($"Расписание на неделю: создано {result.SlotsCount} слотов");
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
                    ScheduleSlots?.Clear();
                    var now = DateTime.Now;
                    if (schedule?.Slots != null)
                    {
                        foreach (var slot in schedule.Slots)
                        {
                            var slotEnd = SelectedDate.Date + slot.EndTime;
                            if (slotEnd < now && !slot.IsBooked)
                                continue;
                            ScheduleSlots?.Add(new ScheduleSlot(this)
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
                    return Task.CompletedTask;
                });
            }
            catch (Exception ex)
            {
                SetError($"Ошибка загрузки расписания: {ex.Message}");
            }
        }

        private void SortScheduleSlots()
        {
            var sorted = ScheduleSlots?.OrderBy(s => s.StartTime).ToList();
            if(sorted != null)
            {
                ScheduleSlots = new ObservableCollection<ScheduleSlot>(sorted);
            }
            
        }

        private async Task LoadCalendarDataAsync()
        {
            try
            {
                if(CalendarDays == null)
                {
                    return;
                }
                var startDate = new DateTime(_currentMonth.Year, _currentMonth.Month, 1);
                var endDate = startDate.AddMonths(1).AddDays(-1);

                var calendarStats = await ScheduleContext.GetCalendarStats(_token, _profileId, startDate, endDate);

                var dateStats = calendarStats?.ToDictionary(s => s.Date, s => s.SlotCount) ?? [];

                foreach (var day in CalendarDays.Where(d => d.Day > 0))
                {
                    if (dateStats.TryGetValue(day.Date, out int value))
                    {
                        day.HasSlots = true;
                        day.SlotCount = value;
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
            if (CalendarDays == null)
            {
                return;
            }
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
            if (CalendarDays == null)
            {
                return;
            }
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
        /// <summary>
        /// Изменить порядок слотов (только в UI)
        /// </summary>
        public async Task ReorderSlotsAsync(ScheduleSlot draggedSlot, ScheduleSlot targetSlot)
        {
            if (draggedSlot == null || targetSlot == null) return;

            try
            {
                int draggedIndex = ScheduleSlots?.IndexOf(draggedSlot) ?? -1;
                int targetIndex = ScheduleSlots?.IndexOf(targetSlot) ?? -1;

                if (draggedIndex != -1 && targetIndex != -1 && draggedIndex != targetIndex)
                {
                    ScheduleSlots?.Move(draggedIndex, targetIndex);
                    Base.Messenger.Default.SendNotification("Порядок слотов изменен");
                }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                SetError($"Ошибка изменения порядка слотов: {ex.Message}");
            }
        }

        // Вычисляемые свойства
        public string DateDisplay => SelectedDate.ToString("dddd, dd MMMM yyyy");
        public string SelectedDateDisplay => SelectedDate.ToString("dd MMMM yyyy");
        public string CurrentMonthYear => _currentMonth.ToString("MMMM yyyy");
        public int TotalSlots => ScheduleSlots?.Count ?? 0;
        public int AvailableSlots => ScheduleSlots?.Count(s => s.IsAvailable && !s.IsBooked) ?? 0;
        public int BookedSlots => ScheduleSlots?.Count(s => s.IsBooked) ?? 0;
        public string DurationDisplay =>
            $"{NewStartTime:hh\\:mm} - {NewEndTime:hh\\:mm} ({(NewEndTime - NewStartTime).TotalHours:0.##} ч)";
        public bool HasScheduleSlots => ScheduleSlots?.Any() ?? false;

        // Внутренний класс для временных слотов
        public class ScheduleSlot : BaseViewModel
        {
            public ICommand? RemoveTimeSlotCommand { get; }
            public ICommand? ToggleAvailabilityCommand { get; }
            public ScheduleViewModel? _scheduleViewModel;
            public ScheduleSlot(ScheduleViewModel scheduleViewModel)
            {
                _scheduleViewModel = scheduleViewModel;
                RemoveTimeSlotCommand = new RelayCommandAsync(async () => await _scheduleViewModel.RemoveTimeSlotAsync(this), () => true);
                ToggleAvailabilityCommand = new RelayCommandAsync(async () => await _scheduleViewModel.ToggleAvailabilityAsync(this), () => !IsBooked);
                UpdateToggleIcon();
            }
            public void UpdateToggleIcon()
            {
                IconsToggle = IsAvailable
                    ? new BitmapImage(new Uri("/RentAFriendApp;component/Resources/Icons/open_ico.png", UriKind.Relative))
                    : new BitmapImage(new Uri("/RentAFriendApp;component/Resources/Icons/close_ico.png", UriKind.Relative));
                OnPropertyChanged(nameof(IconsToggle));
            }
            public int ScheduleID { get; set; }
            public TimeSpan StartTime { get; set; }
            public TimeSpan EndTime { get; set; }
            private bool _isAvailable;
            public bool IsAvailable
            {
                get => _isAvailable;
                set
                {
                    if (SetProperty(ref _isAvailable, value))
                    {
                        OnPropertyChanged(nameof(Status));
                        OnPropertyChanged(nameof(StatusColor));
                        OnPropertyChanged(nameof(StatusIcon));
                        OnPropertyChanged(nameof(ToggleTooltip));
                        UpdateToggleIcon();
                    }
                }
            }
            public string ToggleTooltip
            {
                get
                {
                    if (IsBooked) return "Забронированный слот нельзя изменить";
                    return IsAvailable ? "Сделать недоступным" : "Сделать доступным";
                }
            }
            private bool _isBooked;
            public bool IsBooked
            {
                get => _isBooked;
                set
                {
                    if (SetProperty(ref _isBooked, value))
                    {
                        OnPropertyChanged(nameof(Status));
                        OnPropertyChanged(nameof(StatusColor));
                        OnPropertyChanged(nameof(StatusIcon));
                    }
                }
            }
            
            public DateTime Date { get; set; }
            public string? BookingStatus { get; set; }
            public string? BookingPurpose { get; set; }
            public BitmapImage? _iconsToggle;
            public BitmapImage? IconsToggle
            {
                get => _iconsToggle;
                set => SetProperty(ref _iconsToggle, value);
            }

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
            public Geometry StatusIcon
            {
                get
                {
                    if (IsBooked)
                        return Geometry.Parse("M19,6.41L17.59,5L12,10.59L6.41,5L5,6.41L10.59,12L5,17.59L6.41,19L12,13.41L17.59,19L19,17.59L13.41,12L19,6.41z");

                    return IsAvailable
                        ? Geometry.Parse("M9,16.2L4.8,12l-1.4,1.4L9,19L21,7l-1.4-1.4L9,16.2z")
                        : Geometry.Parse("M19,6.41L17.59,5L12,10.59L6.41,5L5,6.41L10.59,12L5,17.59L6.41,19L12,13.41L17.59,19L19,17.59L13.41,12L19,6.41z");
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