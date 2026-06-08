using RentAFriendApp.Context;
using RentAFriendApp.Models.ClassesDTO.FriendProfileDTO;
using RentAFriendApp.Models.ClassesDTO.FriendProfileDTO.Response;
using RentAFriendApp.ViewModels.Client;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace RentAFriendApp.Views.Client
{
    public partial class FriendDetailsPage : Page
    {
        private readonly string _token;
        private FPInfoDTO _currentFriend;
        private FriendDetailsViewModel _viewModel;

        public FriendDetailsPage(string token, int friendId)
        {
            InitializeComponent();
            _token = token;
            _currentFriend = FriendProfileContext.GetFriendProfileById(friendId, _token).Result;

            Loaded += Page_Loaded;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Обновляем данные друга через API (если нужно)
                await LoadFriendFromDatabase();

                if (_currentFriend == null)
                {
                    MessageBox.Show("Профиль друга не найден", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);

                    GoBackTo();
                    return;
                }

                // Инициализация ViewModel
                _viewModel = new FriendDetailsViewModel(_token, _currentFriend);
                DataContext = _viewModel;

                // Подписка на события ViewModel
                _viewModel.PropertyChanged += ViewModel_PropertyChanged;

                SetFriendInitials();
                SelectTodayDate();
                await LoadTimeSlotsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadFriendFromDatabase()
        {
            try
            {
                // Получаем актуальные данные профиля через контекст
                var updatedFriend = await FriendProfileContext.GetFriendProfileById(_currentFriend.ProfileID, _token);
                if (updatedFriend != null)
                {
                    _currentFriend = updatedFriend;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка обновления профиля друга: {ex.Message}");
            }
        }

        private void SetFriendInitials()
        {
            if (_currentFriend?.FullName != null)
            {
                var initials = GetInitials(_currentFriend.FullName);
                AvatarText.Text = initials;
            }
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
            else if (parts.Length == 1)
            {
                return parts[0].Length >= 2 ? parts[0].Substring(0, 2).ToUpper() : parts[0].ToUpper();
            }

            return "??";
        }

        // ==================== СОБЫТИЯ КНОПОК ====================

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            GoBackTo();
        }

        private void GoBackTo()
        {
            NavigationService.GoBack();
        }

        private void BookButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Переход на страницу бронирования
                var bookingPage = new BookingPage(_token, _currentFriend);
                var mainWindow = Window.GetWindow(this) as MainWindow;
                if (mainWindow != null)
                {
                    mainWindow.MainFrame.Navigate(bookingPage);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка создания бронирования: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void MessageButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentFriend?.UserID != null)
            {
                var chatPage = new ChatPage(_token, _currentFriend.UserID);
                MainWindow.Instanse.MainFrame.Navigate(chatPage);
            }
        }

        private async void AddToFavoritesButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // TODO: Добавить метод AddToFavorites в FriendProfileContext
                // var result = await FriendProfileContext.AddToFavorites(_token, _currentFriend.ProfileID);

                MessageBox.Show("Добавлено в избранное!", "Успешно",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShareButton_Click(object sender, RoutedEventArgs e)
        {
            var profileLink = $"https://rentafriend.com/friend/{_currentFriend?.ProfileID}";
            try
            {
                Clipboard.SetText(profileLink);
                MessageBox.Show("Ссылка на профиль скопирована в буфер обмена!", "Поделиться",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch
            {
                MessageBox.Show("Не удалось скопировать ссылку", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ViewAllReviewsButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.ViewAllReviewsCommand.Execute(null);
            }
        }

        // ==================== СОБЫТИЯ ВРЕМЕННЫХ СЛОТОВ ====================

        private void DateSlot_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Border dateBorder && dateBorder.Tag != null)
            {
                if (DateTime.TryParse(dateBorder.Tag.ToString(), out DateTime selectedDate))
                {
                    ResetDateSelection();
                    dateBorder.Style = (Style)FindResource("SelectedTimeSlotStyle");

                    if (_viewModel != null)
                    {
                        _viewModel.SelectedDate = selectedDate;
                    }

                    if (dateBorder.Child is StackPanel stackPanel && stackPanel.Children.Count > 1)
                    {
                        if (stackPanel.Children[1] is TextBlock dateText)
                        {
                            SelectedDateText.Text = $"Выбрано: {dateText.Text}";
                        }
                    }
                }
            }
        }

        private void TimeSlot_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Border timeBorder && timeBorder.Tag is string timeSlot)
            {
                ResetTimeSlotSelection();
                timeBorder.Style = (Style)FindResource("SelectedTimeSlotStyle");

                if (_viewModel != null)
                {
                    var timeParts = timeSlot.Split('-');
                    if (timeParts.Length == 2)
                    {
                        if (TimeSpan.TryParse(timeParts[0].Trim(), out var startTime) &&
                            TimeSpan.TryParse(timeParts[1].Trim(), out var endTime))
                        {
                            var slot = _viewModel.AvailableTimeSlots.FirstOrDefault(s => s.StartTime == startTime && s.EndTime == endTime);
                            if (slot != null)
                            {
                                _viewModel.SelectedTimeSlot = slot;
                            }
                        }
                    }
                }
            }
        }

        private void ResetDateSelection()
        {
            if (DatePanel.Children != null)
            {
                foreach (var child in DatePanel.Children)
                {
                    if (child is Border border)
                    {
                        border.Style = (Style)FindResource("TimeSlotStyle");
                    }
                }
            }
        }

        private void ResetTimeSlotSelection()
        {
            if (TimeSlotsPanel.Children != null)
            {
                foreach (var child in TimeSlotsPanel.Children)
                {
                    if (child is Border border && border.Tag != null)
                    {
                        if (border.Tag.ToString().Contains("забронировано"))
                        {
                            border.Style = (Style)FindResource("BookedTimeSlotStyle");
                        }
                        else
                        {
                            border.Style = (Style)FindResource("TimeSlotStyle");
                        }
                    }
                }
            }
        }

        // ==================== СОБЫТИЯ МЫШИ ====================

        private void Border_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
            {
                if (border.Tag != null && border.Tag.ToString().Contains("забронировано"))
                    return;

                border.Background = new SolidColorBrush(Color.FromArgb(255, 232, 245, 232));
            }
        }

        private void Border_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
            {
                if (border.Tag != null && border.Tag.ToString().Contains("забронировано"))
                {
                    border.Background = new SolidColorBrush(Color.FromArgb(255, 245, 245, 245));
                    border.Opacity = 0.6;
                }
                else
                {
                    if (border.Style == (Style)FindResource("SelectedTimeSlotStyle"))
                    {
                        border.Background = new SolidColorBrush(Color.FromArgb(255, 76, 175, 80));
                    }
                    else
                    {
                        border.Background = new SolidColorBrush(Color.FromArgb(255, 240, 249, 240));
                    }
                }
            }
        }

        // ==================== СОБЫТИЯ ЗАГРУЗКИ ====================

        private void SelectTodayDate()
        {
            if (DatePanel.Children != null)
            {
                foreach (var child in DatePanel.Children)
                {
                    if (child is Border border && border.Tag != null)
                    {
                        if (DateTime.TryParse(border.Tag.ToString(), out DateTime date))
                        {
                            if (date.Date == DateTime.Today)
                            {
                                DateSlot_Click(border, null);
                                break;
                            }
                        }
                    }
                }
            }
        }

        private async Task LoadTimeSlotsAsync()
        {
            TimeSlotsPanel.Children.Clear();

            if (_viewModel == null) return;

            // Ждем загрузки слотов из ViewModel
            await Task.Delay(100); // Даем время ViewModel загрузить данные

            foreach (var timeSlot in _viewModel.AvailableTimeSlots)
            {
                var border = new Border
                {
                    Style = (Style)FindResource("TimeSlotStyle"),
                    Tag = $"{timeSlot.StartTime:hh\\:mm} - {timeSlot.EndTime:hh\\:mm}",
                    Margin = new Thickness(4),
                    Cursor = Cursors.Hand
                };

                border.MouseEnter += Border_MouseEnter;
                border.MouseLeave += Border_MouseLeave;
                border.MouseDown += (s, e) => TimeSlot_Click(s, e);

                var textBlock = new TextBlock
                {
                    Text = $"{timeSlot.StartTime:hh\\:mm} - {timeSlot.EndTime:hh\\:mm}",
                    FontSize = 13,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50))
                };

                border.Child = textBlock;
                TimeSlotsPanel.Children.Add(border);
            }

            if (TimeSlotsPanel.Children.Count == 0)
            {
                var messageBlock = new TextBlock
                {
                    Text = "Нет доступных слотов на выбранную дату",
                    FontSize = 14,
                    Foreground = new SolidColorBrush(Color.FromRgb(153, 153, 153)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(20)
                };
                TimeSlotsPanel.Children.Add(messageBlock);
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
            }
        }

        private async void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (_viewModel == null) return;

            if (e.PropertyName == nameof(FriendDetailsViewModel.AvailableTimeSlots))
            {
                await LoadTimeSlotsAsync();
            }
            else if (e.PropertyName == nameof(FriendDetailsViewModel.SelectedTimeSlot))
            {
                UpdateSelectedTimeSlot();
            }
        }

        private void UpdateSelectedTimeSlot()
        {
            if (_viewModel == null) return;

            ResetTimeSlotSelection();

            if (_viewModel.SelectedTimeSlot != null)
            {
                foreach (var child in TimeSlotsPanel.Children)
                {
                    if (child is Border border && border.Tag != null)
                    {
                        var timeString = $"{_viewModel.SelectedTimeSlot.StartTime:hh\\:mm} - {_viewModel.SelectedTimeSlot.EndTime:hh\\:mm}";

                        if (border.Tag.ToString() == timeString)
                        {
                            border.Style = (Style)FindResource("SelectedTimeSlotStyle");
                            break;
                        }
                    }
                }
            }
        }

        // ==================== СОБЫТИЯ ПРОКРУТКИ ====================

        private void ReviewsScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            // Ленивая загрузка отзывов при прокрутке
            if (e.VerticalOffset + e.ViewportHeight >= e.ExtentHeight - 100)
            {
                // Можно загрузить еще отзывы
            }
        }

        // ==================== КОНТЕКСТНОЕ МЕНЮ ====================

        private void ReviewBorder_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border reviewBorder)
            {
                var contextMenu = new ContextMenu();

                var copyItem = new MenuItem
                {
                    Header = "Копировать текст отзыва"
                };
                copyItem.Click += (s, args) =>
                {
                    if (reviewBorder.Child is StackPanel panel)
                    {
                        foreach (var child in panel.Children)
                        {
                            if (child is TextBlock textBlock && textBlock.Text.Length > 50)
                            {
                                try
                                {
                                    Clipboard.SetText(textBlock.Text);
                                    MessageBox.Show("Текст отзыва скопирован", "Успешно",
                                        MessageBoxButton.OK, MessageBoxImage.Information);
                                }
                                catch
                                {
                                    MessageBox.Show("Не удалось скопировать текст", "Ошибка",
                                        MessageBoxButton.OK, MessageBoxImage.Error);
                                }
                                break;
                            }
                        }
                    }
                };

                contextMenu.Items.Add(copyItem);
                reviewBorder.ContextMenu = contextMenu;
            }
        }
    }
}