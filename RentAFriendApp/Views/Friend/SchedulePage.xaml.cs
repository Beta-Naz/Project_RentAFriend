using RentAFriendApp.ViewModels.Base;
using RentAFriendApp.ViewModels.Friend;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace RentAFriendApp.Views.Friend
{
    public partial class SchedulePage : Page
    {
        private readonly string _token;
        private readonly ScheduleViewModel _viewModel;
        private DateTime _currentMonth = DateTime.Today;
        private string _lastToastMessage = string.Empty;
        private DateTime _lastToastTime;
        private StackPanel? _toastPanel;

        public SchedulePage(string token)
        {
            InitializeComponent();
            _token = token;
            Unloaded += Page_Unloaded;

            _viewModel = new ScheduleViewModel(_token);
            DataContext = _viewModel;

            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
            Messenger.Default.NotificationReceived += OnNotificationReceived;

            _ = _viewModel.InitializeAsync();

            StartPageAnimation();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            try
            {
                Messenger.Default.NotificationReceived -= OnNotificationReceived;

                if (_viewModel != null)
                {
                    _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
                }

                ClearResources();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SchedulePage.Unloaded: {ex.Message}");
            }
        }

        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    switch (e.PropertyName)
                    {
                        case nameof(ScheduleViewModel.SelectedDate):
                            AnimateDateChange();
                            UpdateCalendar();
                            break;

                        case nameof(ScheduleViewModel.ScheduleSlots):
                            AnimateSlotsUpdate();
                            break;

                        case nameof(ScheduleViewModel.IsBusy):
                            UpdateBusyState();
                            break;

                        case nameof(ScheduleViewModel.HasError):
                            ShowErrorIfNeeded();
                            break;
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SchedulePage.PropertyChanged: {ex.Message}");
            }
        }

        private void StartPageAnimation()
        {
            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromSeconds(0.3),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            BeginAnimation(OpacityProperty, fadeIn);
        }

        private void AnimateDateChange()
        {
            if (FindName("DateDisplayText") is TextBlock dateText)
            {
                var scaleAnimation = new DoubleAnimation
                {
                    From = 0.9,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(0.2),
                    EasingFunction = new ElasticEase { EasingMode = EasingMode.EaseOut }
                };

                dateText.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation);
                dateText.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation);
            }
        }

        private void AnimateSlotsUpdate()
        {
            if (ScheduleSlotsControl?.Items.Count > 0)
            {
                int delay = 0;
                foreach (var item in ScheduleSlotsControl.Items)
                {
                    if (ScheduleSlotsControl.ItemContainerGenerator.ContainerFromItem(item) is FrameworkElement container)
                    {
                        container.Opacity = 0;

                        var animation = new DoubleAnimation
                        {
                            To = 1,
                            Duration = TimeSpan.FromSeconds(0.2),
                            BeginTime = TimeSpan.FromMilliseconds(delay)
                        };

                        container.BeginAnimation(OpacityProperty, animation);
                        delay += 50;
                    }
                }
            }
        }

        private void UpdateCalendar()
        {
            var today = DateTime.Today;
            var selectedDate = _viewModel?.SelectedDate ?? today;

            if (CalendarDaysControl?.Items == null) return;

            foreach (var item in CalendarDaysControl.Items)
            {
                if (CalendarDaysControl.ItemContainerGenerator.ContainerFromItem(item) is Button button)
                {
                    button.Style = (Style)FindResource("DayButtonStyle");

                    if (item is ScheduleViewModel.CalendarDay day)
                    {
                        if (day.Date.Date == today.Date)
                        {
                            button.Style = (Style)FindResource("TodayButtonStyle");
                        }
                        else if (day.Date.Date == selectedDate.Date)
                        {
                            button.Style = (Style)FindResource("SelectedDayButtonStyle");
                        }

                        button.ToolTip = $"{day.Date:dd MMMM yyyy}\nСлотов: {day.SlotCount}";
                    }
                }
            }
        }

        private void UpdateBusyState()
        {
            if (_viewModel != null)
            {
                Cursor = _viewModel.IsBusy ? Cursors.Wait : Cursors.Arrow;

                var buttons = new[] { PrevDayBtn, NextDayBtn, TodayBtn, GenerateWeekBtn };
                foreach (var button in buttons)
                {
                    if (button != null)
                        button.IsEnabled = !_viewModel.IsBusy;
                }
            }
        }

        private void ShowErrorIfNeeded()
        {
            if (_viewModel is { HasError: true })
            {
                var timer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(5)
                };

                timer.Tick += (s, e) =>
                {
                    timer.Stop();
                    DismissError_Click(this, null!);
                };

                timer.Start();
            }
        }

        private void PreviousMonth_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _currentMonth = _currentMonth.AddMonths(-1);
                _viewModel?.UpdateCalendarMonth(_currentMonth);
                AnimateMonthChange(false);
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Ошибка переключения месяца: {ex.Message}");
            }
        }

        private void NextMonth_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _currentMonth = _currentMonth.AddMonths(1);
                _viewModel?.UpdateCalendarMonth(_currentMonth);
                AnimateMonthChange(true);
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Ошибка переключения месяца: {ex.Message}");
            }
        }

        private void AnimateMonthChange(bool forward)
        {
            if (FindName("MonthYearText") is TextBlock monthText)
            {
                var slideAnimation = new ThicknessAnimation
                {
                    From = new Thickness(forward ? -50 : 50, 0, forward ? 50 : -50, 0),
                    To = new Thickness(0),
                    Duration = TimeSpan.FromSeconds(0.3),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };

                monthText.BeginAnimation(MarginProperty, slideAnimation);
            }
        }

        private void DismissError_Click(object? sender, RoutedEventArgs? e)
        {
            _viewModel?.ClearErrors();

            if (ErrorContainer != null)
            {
                var fadeOut = new DoubleAnimation
                {
                    To = 0,
                    Duration = TimeSpan.FromSeconds(0.2)
                };

                ErrorContainer.BeginAnimation(OpacityProperty, fadeOut);
            }
        }

        private void ShowErrorMessage(string message)
        {
            Dispatcher.Invoke(() =>
            {
                _viewModel?.SetError(message);

                if (ErrorContainer != null)
                {
                    ErrorContainer.Visibility = Visibility.Visible;
                    ErrorContainer.Opacity = 1;
                }
            });
        }

        private void ClearResources()
        {
            BeginAnimation(OpacityProperty, null);
            DataContext = null;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (_viewModel is not { IsBusy: false }) return;

            switch (e.Key)
            {
                case Key.Left:
                    _viewModel.PreviousDayCommand.Execute(null);
                    e.Handled = true;
                    break;

                case Key.Right:
                    _viewModel.NextDayCommand.Execute(null);
                    e.Handled = true;
                    break;

                case Key.T when Keyboard.Modifiers == ModifierKeys.Control:
                    _viewModel.TodayCommand.Execute(null);
                    e.Handled = true;
                    break;

                case Key.Add or Key.OemPlus when Keyboard.Modifiers == ModifierKeys.Control:
                    _viewModel.AddTimeSlotCommand.Execute(null);
                    e.Handled = true;
                    break;
            }
        }

        private void OnNotificationReceived(object? sender, string message)
        {
            Dispatcher.Invoke(() =>
            {
                if (message == _lastToastMessage && (DateTime.Now - _lastToastTime).TotalSeconds < 1)
                {
                    return;
                }

                _lastToastMessage = message;
                _lastToastTime = DateTime.Now;
                ShowToast(message, "#2196F3");
            });
        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.NewSize.Width < 1000 && CalendarDaysControl != null)
            {
                CalendarDaysControl.Margin = new Thickness(0, 10, 0, 10);
            }
            else if (CalendarDaysControl != null)
            {
                CalendarDaysControl.Margin = new Thickness(0);
            }
        }

        private void InitializeToastPanel()
        {
            if (Content is Grid grid)
            {
                _toastPanel = new StackPanel
                {
                    VerticalAlignment = VerticalAlignment.Top,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(20),
                    FlowDirection = FlowDirection.RightToLeft
                };

                Panel.SetZIndex(_toastPanel, 1000);
                grid.Children.Add(_toastPanel);
            }
        }

        private void ShowToast(string message, string color)
        {
            if (_toastPanel == null)
            {
                InitializeToastPanel();
            }

            var toast = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(15, 10, 15, 10),
                Margin = new Thickness(0, 0, 0, 10),
                Opacity = 0,
                Child = new TextBlock
                {
                    Text = message,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Foreground = Brushes.White,
                    FontSize = 14,
                    FontWeight = FontWeights.SemiBold,
                    TextWrapping = TextWrapping.Wrap,
                    MaxWidth = 300
                }
            };

            _toastPanel.Children.Add(toast);

            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.3));
            toast.BeginAnimation(OpacityProperty, fadeIn);

            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2.5) };
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.3));
                fadeOut.Completed += (_, _) => _toastPanel.Children.Remove(toast);
                toast.BeginAnimation(OpacityProperty, fadeOut);
            };
            timer.Start();
        }
    }
}