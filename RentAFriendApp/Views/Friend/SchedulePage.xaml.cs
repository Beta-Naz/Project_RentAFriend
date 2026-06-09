using RentAFriendApp.ViewModels.Base;
using RentAFriendApp.ViewModels.Friend;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace RentAFriendApp.Views.Friend
{
    public partial class SchedulePage : Page
    {
        private readonly string _token;
        private ScheduleViewModel _viewModel;
        private DateTime _currentMonth = DateTime.Today;

        public SchedulePage(string token)
        {
            InitializeComponent();
            _token = token;
            Unloaded += Page_Unloaded;

            _viewModel = new ScheduleViewModel(_token);
            DataContext = _viewModel;

            _viewModel.PropertyChanged += ViewModel_PropertyChanged;

            _ = _viewModel.InitializeAsync();

            StartPageAnimation();
            SetupDragAndDrop();
            SubscribeToGlobalEvents();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_viewModel != null)
                {
                    _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
                }

                UnsubscribeFromGlobalEvents();
                ClearResources();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SchedulePage.Unloaded: {ex.Message}");
            }
        }

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
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
            var dateText = this.FindName("DateDisplayText") as TextBlock;
            if (dateText != null)
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
                    var container = ScheduleSlotsControl.ItemContainerGenerator.ContainerFromItem(item) as FrameworkElement;
                    if (container != null)
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
            if (_viewModel != null && _viewModel.HasError)
            {
                var timer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(5)
                };

                timer.Tick += (s, e) =>
                {
                    timer.Stop();
                    DismissError_Click(null, null);
                };

                timer.Start();
            }
        }

        private void SetupDragAndDrop()
        {
            if (ScheduleSlotsControl != null)
            {
                ScheduleSlotsControl.PreviewMouseLeftButtonDown += ScheduleSlot_MouseDown;
                ScheduleSlotsControl.PreviewMouseMove += ScheduleSlot_MouseMove;
                ScheduleSlotsControl.Drop += ScheduleSlot_Drop;
                ScheduleSlotsControl.DragEnter += ScheduleSlot_DragEnter;
                ScheduleSlotsControl.DragLeave += ScheduleSlot_DragLeave;
            }
        }

        private void ScheduleSlot_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is DependencyObject source)
            {
                if (FindParent<Button>(source) != null)
                {
                    return;
                }

                var slot = FindParent<Border>(source);
                if (slot != null && slot.DataContext is ScheduleViewModel.ScheduleSlot)
                {
                    DragDrop.DoDragDrop(slot, slot.DataContext, DragDropEffects.Move);
                    e.Handled = true;
                }
            }
        }

        private void ScheduleSlot_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (e.OriginalSource is FrameworkElement source)
                {
                    var slot = FindParent<Border>(source);
                    if (slot != null)
                    {
                        var dragData = new DataObject(typeof(ScheduleViewModel.ScheduleSlot), slot.DataContext);
                        DragDrop.DoDragDrop(slot, dragData, DragDropEffects.Move);
                    }
                }
            }
        }

        private void ScheduleSlot_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(ScheduleViewModel.ScheduleSlot)))
            {
                if (sender is Border border)
                {
                    border.Background = new SolidColorBrush(Color.FromArgb(30, 76, 175, 80));
                }
                e.Effects = DragDropEffects.Move;
            }
        }

        private void ScheduleSlot_DragLeave(object sender, DragEventArgs e)
        {
            if (sender is Border border)
            {
                var slot = border.DataContext as ScheduleViewModel.ScheduleSlot;
                if (slot != null)
                {
                    border.Background = slot.IsBooked ?
                        new SolidColorBrush(Color.FromArgb(255, 255, 245, 245)) :
                        new SolidColorBrush(Color.FromArgb(255, 240, 249, 240));
                }
            }
        }

        private async void ScheduleSlot_Drop(object sender, DragEventArgs e)
        {
            try
            {
                if (e.Data.GetData(typeof(ScheduleViewModel.ScheduleSlot)) is ScheduleViewModel.ScheduleSlot draggedSlot &&
                    sender is Border targetBorder &&
                    targetBorder.DataContext is ScheduleViewModel.ScheduleSlot targetSlot &&
                    _viewModel != null)
                {
                    await _viewModel.ReorderSlotsAsync(draggedSlot, targetSlot);
                    AnimateSuccessfulDrop(targetBorder);
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Ошибка при изменении порядка: {ex.Message}");
            }
        }

        private void AnimateSuccessfulDrop(FrameworkElement element)
        {
            var animation = new DoubleAnimation
            {
                From = 1,
                To = 1.05,
                Duration = TimeSpan.FromSeconds(0.1),
                AutoReverse = true,
                EasingFunction = new ElasticEase { EasingMode = EasingMode.EaseOut }
            };

            var scaleTransform = new ScaleTransform();
            element.RenderTransform = scaleTransform;
            element.RenderTransformOrigin = new Point(0.5, 0.5);

            scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, animation);
            scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, animation);
        }

        private T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            while (child != null && !(child is T))
            {
                child = VisualTreeHelper.GetParent(child);
            }
            return child as T;
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
            var monthText = this.FindName("MonthYearText") as TextBlock;
            if (monthText != null)
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

        private void DismissError_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.ClearErrors();
            }

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
                if (_viewModel != null)
                {
                    _viewModel.SetError(message);
                }

                if (ErrorContainer != null)
                {
                    ErrorContainer.Visibility = Visibility.Visible;
                    ErrorContainer.Opacity = 1;
                }
            });
        }

        private void SubscribeToGlobalEvents()
        {
            // Подписка на глобальные события приложения
        }

        private void UnsubscribeFromGlobalEvents()
        {
            // Отписка от глобальных событий
        }

        private void ClearResources()
        {
            BeginAnimation(OpacityProperty, null);
            DataContext = null;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (_viewModel == null || _viewModel.IsBusy)
                return;

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

                case Key.T:
                    if (Keyboard.Modifiers == ModifierKeys.Control)
                    {
                        _viewModel.TodayCommand.Execute(null);
                        e.Handled = true;
                    }
                    break;

                case Key.Add:
                case Key.OemPlus:
                    if (Keyboard.Modifiers == ModifierKeys.Control)
                    {
                        _viewModel.AddTimeSlotCommand.Execute(null);
                        e.Handled = true;
                    }
                    break;
            }
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
    }
}