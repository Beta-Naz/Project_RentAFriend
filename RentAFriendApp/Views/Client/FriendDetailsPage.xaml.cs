using RentAFriendApp.Context;
using RentAFriendApp.ViewModels.Client;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace RentAFriendApp.Views.Client
{
    public partial class FriendDetailsPage : Page
    {
        private readonly string _token;
        private readonly int _profileId;
        private FriendDetailsViewModel? _vm;
        private readonly List<Border> _dateBorders = new();
        private readonly List<Border> _timeBorders = new();

        public FriendDetailsPage(string token, int profileId)
        {
            InitializeComponent();
            _token = token;
            _profileId = profileId;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            var resp = await FriendProfileContext.GetFriendProfileById(_profileId, _token);
            if (resp?.Profile == null) { NavigationService?.GoBack(); return; }

            _vm = new FriendDetailsViewModel(_token, resp.Profile);
            DataContext = _vm;

            _vm.PropertyChanged += (s, args) =>
            {
                if (args.PropertyName == nameof(_vm.Reviews))
                    UpdateReviews();
            };

            LoadHobbies();
            await _vm.LoadReviewsAsync();
            UpdateReviews();
            await BuildWeekGridAsync();
        }

        private void LoadHobbies()
        {
            HobbiesPanel.Children.Clear();
            if (string.IsNullOrWhiteSpace(_vm?.FriendHobbies)) return;
            foreach (var h in _vm.FriendHobbies.Split(',').Select(x => x.Trim()).Take(6))
                HobbiesPanel.Children.Add(new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(232, 245, 232)),
                    CornerRadius = new CornerRadius(12),
                    Padding = new Thickness(10, 5, 10, 5),
                    Margin = new Thickness(0, 0, 6, 6),
                    Child = new TextBlock { Text = h, FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50)) }
                });
        }

        private void UpdateReviews()
        {
            if (_vm?.Reviews == null) return;
            var items = _vm.Reviews.Select(r => new
            {
                ReviewerName = "Клиент",
                ReviewerInitials = GetInitials("Клиент"),
                r.Comment,
                CreatedAtDisplay = r.CreatedAt.ToString("dd MMMM yyyy"),
                Stars = Enumerable.Range(0, (int)r.Rating)
            }).ToList();
            ReviewsList.ItemsSource = items;
        }

        private async Task BuildWeekGridAsync()
        {
            WeekGrid.Children.Clear();
            _dateBorders.Clear();

            var today = DateTime.Today;
            var dayNames = new[] { "Вс", "Пн", "Вт", "Ср", "Чт", "Пт", "Сб" };

            var meetings = await FriendProfileContext.GetUpcomingMeetings(_token, _profileId, top: 50);
            var availableDates = new HashSet<DateTime>();
            if (meetings?.Meetings != null)
                foreach (var m in meetings.Meetings)
                    availableDates.Add(m.ScheduleDate.Date);

            Border? todayBorder = null;

            for (int i = 0; i < 7; i++)
            {
                var date = today.AddDays(i);
                var dayName = dayNames[(int)date.DayOfWeek];
                bool hasSlots = availableDates.Contains(date);
                bool isToday = date == today;

                var container = new Border
                {
                    Style = (Style)FindResource(isToday ? "SelectedTimeSlotStyle" : "TimeSlotStyle"),
                    Tag = date,
                    Margin = new Thickness(3),
                    Cursor = Cursors.Hand,
                    Child = new StackPanel
                    {
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(4),
                        Children =
                {
                    new TextBlock
                    {
                        Text = dayName,
                        FontSize = 11,
                        FontWeight = isToday ? FontWeights.Bold : FontWeights.Normal,
                        Foreground = isToday ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Color.FromRgb(117, 117, 117)),
                        HorizontalAlignment = HorizontalAlignment.Center
                    },
                    new TextBlock
                    {
                        Text = date.ToString("dd"),
                        FontSize = 18,
                        FontWeight = FontWeights.Bold,
                        Foreground = isToday ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Color.FromRgb(66, 66, 66)),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 2, 0, 0)
                    },
                    new TextBlock
                    {
                        Text = date.ToString("MMM"),
                        FontSize = 10,
                        Foreground = isToday ? new SolidColorBrush(Color.FromRgb(200, 230, 200)) : new SolidColorBrush(Color.FromRgb(158, 158, 158)),
                        HorizontalAlignment = HorizontalAlignment.Center
                    },
                    new Border
                    {
                        Width = 6, Height = 6, CornerRadius = new CornerRadius(3),
                        Background = hasSlots ? new SolidColorBrush(Color.FromRgb(76, 175, 80)) : new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 4, 0, 0)
                    }
                }
                    }
                };

                var currentDate = date;
                container.MouseDown += (s, ev) => SelectDate(currentDate, container);
                Grid.SetColumn(container, i);
                WeekGrid.Children.Add(container);
                _dateBorders.Add(container);

                if (isToday) todayBorder = container;
            }

            if (todayBorder != null)
                SelectDate(today, todayBorder);
            else if (_dateBorders.Count > 0)
                SelectDate((DateTime)_dateBorders.First().Tag, _dateBorders.First());
        }

        private async void SelectDate(DateTime date, Border? target)
        {
            foreach (var b in _dateBorders)
            {
                var isTarget = b == target;
                b.Style = isTarget ? (Style)FindResource("SelectedTimeSlotStyle") : (Style)FindResource("TimeSlotStyle");

                if (b.Child is StackPanel sp && sp.Children.Count >= 3)
                {
                    var dayColor = isTarget ? Colors.White : Color.FromRgb(117, 117, 117);
                    var dateColor = isTarget ? Colors.White : Color.FromRgb(66, 66, 66);
                    var monthColor = isTarget ? Color.FromRgb(200, 230, 200) : Color.FromRgb(158, 158, 158);

                    if (sp.Children[0] is TextBlock dayTb) dayTb.Foreground = new SolidColorBrush(dayColor);
                    if (sp.Children[1] is TextBlock dateTb) dateTb.Foreground = new SolidColorBrush(dateColor);
                    if (sp.Children[2] is TextBlock monthTb) monthTb.Foreground = new SolidColorBrush(monthColor);
                }
            }

            SelectedDateLabel.Text = date.ToString("dd MMMM, dddd");
            _vm!.SelectedDate = date;
            await Task.Delay(200);
            await LoadSlotsAsync(date);
        }

        private async Task LoadSlotsAsync(DateTime date)
        {
            TimeSlotsPanel.Children.Clear();
            _timeBorders.Clear();
            SummaryPanel.Visibility = Visibility.Collapsed;
            _vm!.SelectedTimeSlot = null;

            _vm.SelectedDate = date;
            await Task.Delay(150);

            var slots = _vm.AvailableTimeSlots;
            bool anyAvailable = false;
            int totalSlots = slots.Count;
            int availableSlots = 0;

            foreach (var slot in slots)
            {
                bool isBooked = slot.IsBooked || !slot.IsAvailable;
                var style = isBooked ? (Style)FindResource("BookedTimeSlotStyle") : (Style)FindResource("TimeSlotStyle");

                var border = new Border
                {
                    Style = style,
                    Tag = slot,
                    Child = new StackPanel
                    {
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Children =
                        {
                            new TextBlock
                            {
                                Text = $"{slot.StartTime:hh\\:mm} – {slot.EndTime:hh\\:mm}",
                                FontSize = 13,
                                FontWeight = FontWeights.SemiBold,
                                Foreground = isBooked
                                    ? new SolidColorBrush(Color.FromRgb(189, 189, 189))
                                    : new SolidColorBrush(Color.FromRgb(46, 125, 50)),
                                HorizontalAlignment = HorizontalAlignment.Center
                            },
                            new TextBlock
                            {
                                Text = isBooked ? "Занято" : "Свободно",
                                FontSize = 10,
                                Foreground = isBooked
                                    ? new SolidColorBrush(Color.FromRgb(189, 189, 189))
                                    : new SolidColorBrush(Color.FromRgb(129, 199, 132)),
                                HorizontalAlignment = HorizontalAlignment.Center,
                                Margin = new Thickness(0, 2, 0, 0)
                            }
                        }
                    }
                };

                if (!isBooked)
                {
                    var capturedSlot = slot;
                    border.MouseDown += (s, ev) => SelectSlot(capturedSlot, border);
                    anyAvailable = true;
                    availableSlots++;
                }

                TimeSlotsPanel.Children.Add(border);
                _timeBorders.Add(border);
            }

            NoSlotsPanel.Visibility = anyAvailable ? Visibility.Collapsed : Visibility.Visible;
            SlotsSummaryText.Text = anyAvailable
                ? $"Свободно {availableSlots} из {totalSlots} слотов"
                : "Нет доступных слотов";

            if (!anyAvailable)
            {
                NoSlotsMessageText.Text = date.Date == DateTime.Today
                    ? "На сегодня свободных слотов нет"
                    : "Нет доступных слотов на этот день";
            }
        }

        private void SelectSlot(FriendDetailsViewModel.ScheduleSlot slot, Border border)
        {
            _vm!.SelectedTimeSlot = slot;

            foreach (var b in _timeBorders)
            {
                if (b.Tag is FriendDetailsViewModel.ScheduleSlot s && s.IsAvailable)
                {
                    b.Style = (Style)FindResource("TimeSlotStyle");
                    if (b.Child is StackPanel sp && sp.Children.Count > 0 && sp.Children[0] is TextBlock tb)
                    {
                        tb.Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50));
                        if (sp.Children.Count > 1 && sp.Children[1] is TextBlock stb)
                            stb.Foreground = new SolidColorBrush(Color.FromRgb(129, 199, 132));
                    }
                }
            }

            border.Style = (Style)FindResource("SelectedTimeSlotStyle");
            if (border.Child is StackPanel sp2)
            {
                if (sp2.Children.Count > 0 && sp2.Children[0] is TextBlock tb2)
                    tb2.Foreground = new SolidColorBrush(Colors.White);
                if (sp2.Children.Count > 1 && sp2.Children[1] is TextBlock stb2)
                    stb2.Foreground = new SolidColorBrush(Color.FromRgb(200, 230, 200));
            }

            SummaryPanel.Visibility = Visibility.Visible;
            SummaryTimeText.Text = $"{slot.StartTime:hh\\:mm} – {slot.EndTime:hh\\:mm}";
            var duration = slot.EndTime - slot.StartTime;
            SummaryDurationText.Text = duration.TotalHours >= 1
                ? $"{(int)duration.TotalHours} ч {duration.Minutes} мин"
                : $"{duration.Minutes} мин";

            if (_vm.CurrentFriend?.HourlyRate.HasValue == true)
                SummaryPriceText.Text = $"{_vm.CurrentFriend.HourlyRate.Value * (decimal)duration.TotalHours:N0} ₽";
            else
                SummaryPriceText.Text = "Бесплатно";
        }

        private static string GetInitials(string? n)
        {
            if (string.IsNullOrWhiteSpace(n)) return "?";
            var p = n.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return p.Length >= 2 ? $"{p[0][0]}{p[^1][0]}".ToUpper() : n[..Math.Min(2, n.Length)].ToUpper();
        }

        private void BookButton_Click(object sender, RoutedEventArgs e)
        {
            if (_vm?.SelectedTimeSlot == null)
            {
                MessageBox.Show("Выберите время встречи", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var bookingPage = new BookingPage(_token, _vm.CurrentFriend!)
            {
                PreSelectedDate = _vm.SelectedDate,
                PreSelectedSlot = new TimeSlotInfo
                {
                    ScheduleID = _vm.SelectedTimeSlot.ScheduleID,
                    StartTime = _vm.SelectedTimeSlot.StartTime,
                    EndTime = _vm.SelectedTimeSlot.EndTime,
                    Duration = _vm.SelectedTimeSlot.EndTime - _vm.SelectedTimeSlot.StartTime,
                    TotalAmount = _vm.CurrentFriend?.HourlyRate.HasValue == true
                        ? _vm.CurrentFriend.HourlyRate.Value * (decimal)(_vm.SelectedTimeSlot.EndTime - _vm.SelectedTimeSlot.StartTime).TotalHours
                        : 0,
                    IsSelected = true
                }
            };

            (Window.GetWindow(this) as MainWindow)?.MainFrame.Navigate(bookingPage);
        }

        private void MessageButton_Click(object sender, RoutedEventArgs e)
        {
            if (_vm?.CurrentFriend?.UserID != null)
                (Window.GetWindow(this) as MainWindow)?.MainFrame.Navigate(new ChatPage(_token, _vm.CurrentFriend.UserID));
        }

        private void BackButton_Click(object sender, RoutedEventArgs e) => NavigationService?.GoBack();
        private void ShareButton_Click(object sender, RoutedEventArgs e)
        {
            try { Clipboard.SetText($"https://rentafriend.com/friend/{_profileId}"); MessageBox.Show("Ссылка скопирована!"); }
            catch { }
        }
        private void ViewAllReviewsButton_Click(object sender, RoutedEventArgs e) { }
        private void ReviewsScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e) { }
        private void ReviewBorder_MouseRightButtonDown(object sender, MouseButtonEventArgs e) { }
        private void Page_Unloaded(object sender, RoutedEventArgs e) { }
    }
}