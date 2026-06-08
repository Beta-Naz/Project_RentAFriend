using RentAFriendApp.Context;
using RentAFriendApp.Models.ClassesDTO.BookingDTO;
using RentAFriendApp.Models.ClassesDTO.FriendProfileDTO;
using RentAFriendApp.Models.ClassesDTO.FriendProfileDTO.Response;
using RentAFriendApp.Models.ClassesDTO.ScheduleDTO;
using RentAFriendApp.ViewModels.Client;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace RentAFriendApp.Views.Client
{
    public partial class BookingPage : Page
    {
        private readonly string _token;
        private BookingViewModel _viewModel;
        private FPInfoDTO _friendProfile;
        private DateTime _currentDate = DateTime.Today;
        private ObservableCollection<TimeSlotInfo> _availableTimeSlots = new ObservableCollection<TimeSlotInfo>();
        private DispatcherTimer _validationTimer;
        private TimeSlotInfo _selectedTimeSlot;
        private const double COMMISSION_PERCENTAGE = 0.15;
        private bool isActive = false;

        public BookingPage(string token, FPInfoDTO friend)
        {
            InitializeComponent();
            isActive = true;
            _token = token;
            _friendProfile = friend;

            _validationTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(1000),
                IsEnabled = false
            };
            _validationTimer.Tick += ValidationTimer_Tick;

            ShowLoading(true);

            Dispatcher.BeginInvoke(new Action(async () =>
            {
                await LoadDataAsync();
            }), DispatcherPriority.Background);
        }

        private async System.Threading.Tasks.Task LoadDataAsync()
        {
            try
            {
                // Обновляем профиль через API
                var updatedProfile = await FriendProfileContext.GetFriendProfileById(_friendProfile.ProfileID, _token);
                if (updatedProfile != null)
                {
                    _friendProfile = updatedProfile;
                }

                if (_friendProfile == null)
                {
                    Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show("Профиль друга не найден", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        NavigationService?.GoBack();
                    });
                    return;
                }

                _viewModel = new BookingViewModel(_token, _friendProfile);

                Dispatcher.Invoke(() =>
                {
                    DataContext = _viewModel;
                    UpdateFriendInfo(_friendProfile);
                    UpdateDateDisplay();
                    ShowLoading(false);
                    LoadAvailableTimeSlots();
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    ShowLoading(false);
                    MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    NavigationService?.GoBack();
                });
            }
        }

        private void UpdateFriendInfo(FPInfoDTO friendProfile)
        {
            if (friendProfile == null) return;

            FriendNameText.Text = friendProfile.FullName ?? "Друг";

            var initials = GetInitials(friendProfile.FullName);
            AvatarInitials.Text = initials;

            FriendFullName.Text = friendProfile.FullName ?? "Неизвестный";
            RatingText.Text = friendProfile.AverageRating?.ToString("0.0") ?? "0.0";
            HourlyRateDisplay.Text = $"{friendProfile.HourlyRate:N0} ₽/час";
            CityText.Text = !string.IsNullOrEmpty(friendProfile.City) ? friendProfile.City : "Не указан";
            AgeText.Text = friendProfile.Age.HasValue ? $"{friendProfile.Age} лет" : "Не указан";
            BioText.Text = friendProfile.Bio ?? "Нет описания";

            if (!string.IsNullOrEmpty(friendProfile.Hobbies))
            {
                HobbiesText.Text = friendProfile.Hobbies;
            }
            else
            {
                HobbiesPanel.Visibility = Visibility.Collapsed;
            }

            VerifiedBadge.Visibility = friendProfile.IsVerified ? Visibility.Visible : Visibility.Collapsed;
        }

        private string GetInitials(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return "??";

            var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                return $"{parts[0][0]}{parts[1][0]}".ToUpper();
            }
            else if (parts.Length == 1 && parts[0].Length >= 2)
            {
                return parts[0].Substring(0, 2).ToUpper();
            }

            return fullName.Length >= 2 ? fullName.Substring(0, 2).ToUpper() : fullName.ToUpper();
        }

        private async void LoadAvailableTimeSlots()
        {
            try
            {
                _availableTimeSlots.Clear();
                TimeSlotsPanel.Children.Clear();
                NoSlotsMessage.Visibility = Visibility.Collapsed;

                if (_friendProfile == null) return;

                // Используем ScheduleContext для получения слотов
                var availableSlots = await ScheduleContext.GetAvailableTimeSlots(
                    _friendProfile.ProfileID, _currentDate, _token);

                bool hasSlots = false;

                if (availableSlots?.Slots != null)
                {
                    foreach (var slot in availableSlots.Slots)
                    {
                        hasSlots = true;
                        var duration = slot.EndTime - slot.StartTime;
                        var hourlyRate = _friendProfile?.HourlyRate ?? 0;
                        var totalAmount = hourlyRate * (decimal)duration.TotalHours;

                        var timeSlot = new TimeSlotInfo
                        {
                            ScheduleID = slot.ScheduleID,
                            StartTime = slot.StartTime,
                            EndTime = slot.EndTime,
                            Duration = duration,
                            TotalAmount = totalAmount,
                            IsSelected = false
                        };

                        _availableTimeSlots.Add(timeSlot);
                        CreateTimeSlotControl(timeSlot);
                    }
                }

                if (!hasSlots)
                {
                    NoSlotsMessage.Visibility = Visibility.Visible;
                }

                UpdateDateDisplay();
                ValidateForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки временных слотов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CreateTimeSlotControl(TimeSlotInfo timeSlot)
        {
            var border = new Border
            {
                Style = (Style)FindResource("TimeSlotCardStyle"),
                Margin = new Thickness(0, 0, 10, 10),
                Tag = timeSlot,
                Cursor = Cursors.Hand,
                ToolTip = $"Нажмите для выбора: {timeSlot.StartTime:hh\\:mm} - {timeSlot.EndTime:hh\\:mm}"
            };

            border.MouseDown += TimeSlot_MouseDown;

            var stackPanel = new StackPanel();

            var timeText = new TextBlock
            {
                Text = $"{timeSlot.StartTime:hh\\:mm} - {timeSlot.EndTime:hh\\:mm}",
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.Black
            };

            var hours = timeSlot.Duration.TotalHours;
            var hoursText = hours == 1 ? "час" : hours < 5 ? "часа" : "часов";

            var infoText = new TextBlock
            {
                Text = $"{hours:0.#} {hoursText} • {timeSlot.TotalAmount:N0} ₽",
                FontSize = 12,
                Foreground = Brushes.Gray,
                Margin = new Thickness(0, 4, 0, 0)
            };

            stackPanel.Children.Add(timeText);
            stackPanel.Children.Add(infoText);
            border.Child = stackPanel;

            TimeSlotsPanel.Children.Add(border);
        }

        private void UpdateDateDisplay()
        {
            var culture = new CultureInfo("ru-RU");
            SelectedDateText.Text = _currentDate.ToString("dddd, d MMMM yyyy", culture);

            if (_currentDate.Date == DateTime.Today)
            {
                DateHintText.Text = "Сегодня";
            }
            else if (_currentDate.Date == DateTime.Today.AddDays(1))
            {
                DateHintText.Text = "Завтра";
            }
            else
            {
                DateHintText.Text = "";
            }
        }

        private void BtnPrevDate_Click(object sender, RoutedEventArgs e)
        {
            _currentDate = _currentDate.AddDays(-1);
            LoadAvailableTimeSlots();
        }

        private void BtnNextDate_Click(object sender, RoutedEventArgs e)
        {
            _currentDate = _currentDate.AddDays(1);
            LoadAvailableTimeSlots();
        }

        private void TimeSlot_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is TimeSlotInfo timeSlot)
            {
                ResetTimeSlotStyles();

                border.Style = (Style)FindResource("SelectedTimeSlotStyle");
                timeSlot.IsSelected = true;
                _selectedTimeSlot = timeSlot;

                UpdateBookingSummary(timeSlot);
                ValidateForm();
            }
        }

        private void ResetTimeSlotStyles()
        {
            foreach (var child in TimeSlotsPanel.Children)
            {
                if (child is Border border)
                {
                    border.Style = (Style)FindResource("TimeSlotCardStyle");
                    if (border.Tag is TimeSlotInfo timeSlot)
                    {
                        timeSlot.IsSelected = false;
                    }
                }
            }
            _selectedTimeSlot = null;
        }

        private void UpdateBookingSummary(TimeSlotInfo timeSlot)
        {
            if (timeSlot == null) return;

            var culture = new CultureInfo("ru-RU");
            SelectedDateTimeText.Text = $"{_currentDate:dd MMM}, {timeSlot.StartTime:hh\\:mm}-{timeSlot.EndTime:hh\\:mm}";

            var hours = timeSlot.Duration.TotalHours;
            var hoursText = hours == 1 ? "час" : hours < 5 ? "часа" : "часов";
            DurationText.Text = $"{hours:0.#} {hoursText}";

            var hourlyRate = _friendProfile?.HourlyRate ?? 0;
            HourlyRateText.Text = $"{hourlyRate:N0} ₽/час";
            TotalAmountText.Text = $"{timeSlot.TotalAmount:N0} ₽";

            var commission = timeSlot.TotalAmount * (decimal)COMMISSION_PERCENTAGE;
            var friendAmount = timeSlot.TotalAmount - commission;

            var commissionText = $"В стоимость включена комиссия сервиса {COMMISSION_PERCENTAGE * 100}% ({commission:N0} ₽). " +
                                $"Оставшаяся сумма ({friendAmount:N0} ₽) будет переведена другу после успешного завершения встречи.";

            CommissionTextBlock.Text = commissionText;
        }

        private async void BtnConfirmBooking_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateForm()) return;

            if (_selectedTimeSlot == null)
            {
                MessageBox.Show("Выберите время встречи", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Вы подтверждаете бронирование встречи с {_friendProfile?.FullName ?? "другом"}?\n\n" +
                $"Дата: {_currentDate:dd.MM.yyyy}\n" +
                $"Время: {_selectedTimeSlot.StartTime:hh\\:mm}-{_selectedTimeSlot.EndTime:hh\\:mm}\n" +
                $"Сумма: {_selectedTimeSlot.TotalAmount:N0} ₽",
                "Подтверждение бронирования",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.No) return;

            try
            {
                ShowLoading(true);
                BtnConfirmBooking.IsEnabled = false;
                BtnConfirmBooking.Content = "Создание бронирования...";

                var bookingData = new CreateBookingDTO
                {
                    ScheduleID = _selectedTimeSlot.ScheduleID,
                    Purpose = GetTextBoxValue(PurposeTextBox),
                    MeetingLocation = GetTextBoxValue(LocationTextBox),
                    SpecialRequests = GetTextBoxValue(SpecialRequestsTextBox)
                };

                var bookingResult = await BookingContext.CreateBooking(_token, bookingData);

                if (bookingResult != null)
                {
                    Dispatcher.Invoke(() =>
                    {
                        ShowSuccessAnimation(bookingResult.BookingId, bookingResult.TotalAmount);
                    });
                }
                else
                {
                    throw new Exception("Не удалось создать бронирование");
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    ShowLoading(false);
                    BtnConfirmBooking.IsEnabled = true;
                    BtnConfirmBooking.Content = "Подтвердить бронирование";

                    if (ex.Message.Contains("слот") || ex.Message.Contains("занят"))
                    {
                        MessageBox.Show("К сожалению, выбранный слот больше недоступен.\nПожалуйста, выберите другое время.",
                            "Слот занят", MessageBoxButton.OK, MessageBoxImage.Warning);
                        LoadAvailableTimeSlots();
                    }
                    else
                    {
                        MessageBox.Show($"Ошибка создания бронирования: {ex.Message}",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                });
            }
        }

        private void ShowSuccessAnimation(int bookingId, decimal totalAmount)
        {
            ShowLoading(false);

            var successBorder = new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(25),
                Margin = new Thickness(20),
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                { BlurRadius = 20, Opacity = 0.2, ShadowDepth = 0 },
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Width = 400
            };

            var successStack = new StackPanel();

            var checkIcon = new Border
            {
                Width = 60,
                Height = 60,
                Background = new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                CornerRadius = new CornerRadius(30),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            };

            var checkPath = new System.Windows.Shapes.Path
            {
                Data = Geometry.Parse("M9,16.17L4.83,12L3.41,13.41L9,19L21,7L19.59,5.59L9,16.17Z"),
                Fill = Brushes.White,
                Stretch = Stretch.Uniform,
                Margin = new Thickness(15)
            };
            checkIcon.Child = checkPath;

            var successText = new TextBlock
            {
                Text = "Бронирование успешно создано!",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.Black,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            };

            var detailsText = new TextBlock
            {
                Text = $"ID бронирования: #{bookingId}\n" +
                       $"Сумма: {totalAmount:N0} ₽\n" +
                       $"Статус: Ожидает подтверждения",
                FontSize = 14,
                Foreground = Brushes.Gray,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            };

            var okButton = new Button
            {
                Content = "Отлично!",
                Style = (Style)FindResource("PrimaryButton"),
                Width = 150,
                Height = 40,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            okButton.Click += (s, args) =>
            {
                var home = new ClientHomePage(_token);
                MainWindow.Instanse.MainFrame.Navigate(home);
            };

            successStack.Children.Add(checkIcon);
            successStack.Children.Add(successText);
            successStack.Children.Add(detailsText);
            successStack.Children.Add(okButton);
            successBorder.Child = successStack;

            var overlay = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(180, 0, 0, 0)),
                Child = successBorder
            };
            Panel.SetZIndex(overlay, 1001);
            RootGrid.Children.Add(overlay);
        }

        private bool ValidateForm()
        {
            bool isValid = true;
            var errors = new StringBuilder();

            if (_selectedTimeSlot == null)
            {
                errors.AppendLine("• Выберите время встречи");
                isValid = false;
            }

            var purpose = GetTextBoxValue(PurposeTextBox);
            if (string.IsNullOrWhiteSpace(purpose))
            {
                errors.AppendLine("• Укажите цель встречи");
                isValid = false;
            }
            else if (purpose.Length > 500)
            {
                errors.AppendLine("• Цель встречи не должна превышать 500 символов");
                isValid = false;
            }

            var location = GetTextBoxValue(LocationTextBox);
            if (string.IsNullOrWhiteSpace(location))
            {
                errors.AppendLine("• Укажите место встречи");
                isValid = false;
            }
            else if (location.Length > 200)
            {
                errors.AppendLine("• Место встречи не должно превышать 200 символов");
                isValid = false;
            }

            var specialRequests = GetTextBoxValue(SpecialRequestsTextBox);
            if (specialRequests.Length > 1000)
            {
                errors.AppendLine("• Особые пожелания не должны превышать 1000 символов");
                isValid = false;
            }

            BtnConfirmBooking.IsEnabled = isValid;
            return isValid;
        }

        private string GetTextBoxValue(TextBox textBox)
        {
            if (textBox == null) return string.Empty;

            var text = textBox.Text?.Trim();
            if (textBox.Foreground == Brushes.Gray &&
                (text.Contains("Например:") || text.Contains("Не выбрано")))
            {
                return string.Empty;
            }
            return text ?? string.Empty;
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.GoBack();
        }

        private void PurposeTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && textBox.Foreground == Brushes.Gray)
            {
                textBox.Text = string.Empty;
                textBox.Foreground = Brushes.Black;
            }
        }

        private void PurposeTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && string.IsNullOrWhiteSpace(textBox.Text))
            {
                textBox.Text = "Например: Прогулка в парке, поход в кино, обед в кафе";
                textBox.Foreground = Brushes.Gray;
            }
        }

        private void LocationTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && textBox.Foreground == Brushes.Gray)
            {
                textBox.Text = string.Empty;
                textBox.Foreground = Brushes.Black;
            }
        }

        private void LocationTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && string.IsNullOrWhiteSpace(textBox.Text))
            {
                textBox.Text = "Например: Парк Горького, главный вход";
                textBox.Foreground = Brushes.Gray;
            }
        }

        private void SpecialRequestsTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && textBox.Foreground == Brushes.Gray)
            {
                textBox.Text = string.Empty;
                textBox.Foreground = Brushes.Black;
            }
        }

        private void SpecialRequestsTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && string.IsNullOrWhiteSpace(textBox.Text))
            {
                textBox.Text = "Например: У меня аллергия на кошек, предпочитаю тихие места";
                textBox.Foreground = Brushes.Gray;
            }
        }

        private void PurposeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (isActive)
            {
                var textBox = sender as TextBox;
                if (textBox != null)
                {
                    var text = GetTextBoxValue(textBox);
                    PurposeCounter.Text = $"{text.Length}/500";
                    StartValidationTimer();
                }
            }
        }

        private void LocationTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!isActive) return;

            var textBox = sender as TextBox;
            if (textBox != null)
            {
                var text = GetTextBoxValue(textBox);
                LocationCounter.Text = $"{text.Length}/200";
                StartValidationTimer();
            }
        }

        private void SpecialRequestsTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!isActive) return;

            var textBox = sender as TextBox;
            if (textBox != null)
            {
                var text = GetTextBoxValue(textBox);
                SpecialRequestsCounter.Text = $"{text.Length}/1000";
                StartValidationTimer();
            }
        }

        private void StartValidationTimer()
        {
            _validationTimer.Stop();
            _validationTimer.Start();
        }

        private void ValidationTimer_Tick(object sender, EventArgs e)
        {
            _validationTimer.Stop();
            ValidateForm();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            SetPlaceholderColors();
            ValidateForm();
        }

        private void SetPlaceholderColors()
        {
            if (PurposeTextBox.Text.Contains("Например:"))
                PurposeTextBox.Foreground = Brushes.Gray;

            if (LocationTextBox.Text.Contains("Например:"))
                LocationTextBox.Foreground = Brushes.Gray;

            if (SpecialRequestsTextBox.Text.Contains("Например:"))
                SpecialRequestsTextBox.Foreground = Brushes.Gray;
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            _availableTimeSlots.Clear();
            _validationTimer?.Stop();
        }

        private void ShowLoading(bool show)
        {
            LoadingOverlay.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            IsEnabled = !show;
        }
    }

    internal class TimeSlotInfo
    {
        public int ScheduleID { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public TimeSpan Duration { get; set; }
        public decimal TotalAmount { get; set; }
        public bool IsSelected { get; set; }
    }
}