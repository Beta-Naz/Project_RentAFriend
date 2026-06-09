using RentAFriendApp.Context;
using RentAFriendApp.Models;
using RentAFriendApp.Models.ClassesDTO.BookingDTO.Response;
using RentAFriendApp.Models.ClassesDTO.FriendProfileDTO.Response;
using RentAFriendApp.Models.ClassesDTO.ReviewDTO;
using RentAFriendApp.ViewModels.Base;
using RentAFriendApp.ViewModels.Friend;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RentAFriendApp.Views.Friend
{
    public partial class FriendHomePage : Page
    {
        private readonly string _token;
        private FriendHomeViewModel _viewModel;

        public FriendHomePage(string token)
        {
            InitializeComponent();
            _token = token;

            _viewModel = new FriendHomeViewModel(_token);
            DataContext = _viewModel;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Подписываемся на сообщения мессенджера
                Messenger.Default.NotificationReceived += OnNotificationReceived;
                Messenger.Default.BusyStateChanged += OnBusyStateChanged;

                // Запускаем загрузку данных
                _viewModel.RefreshCommand.Execute(null);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки страницы: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            try
            {
                Messenger.Default.NotificationReceived -= OnNotificationReceived;
                Messenger.Default.BusyStateChanged -= OnBusyStateChanged;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при выгрузке страницы: {ex.Message}");
            }
        }

        private void OnNotificationReceived(object sender, string message)
        {
            Dispatcher.Invoke(() =>
            {
                MessageBox.Show(message, "Уведомление",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            });
        }

        private void OnBusyStateChanged(object sender, bool isBusy)
        {
            Dispatcher.Invoke(() =>
            {
                Cursor = isBusy ? Cursors.Wait : Cursors.Arrow;
            });
        }

        // Обработчики событий для встреч
        private void MeetingItem_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
            {
                border.Cursor = Cursors.Hand;
            }
        }

        private void MeetingItem_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
            {
                border.Cursor = Cursors.Arrow;
            }
        }

        private void MeetingItem_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is UpcomingMeetingItem meeting)
            {
                MessageBox.Show($"Детали встречи:\n\n" +
                              $"Клиент: {meeting.ClientName}\n" +
                              $"Дата: {meeting.ScheduleDate:dd.MM.yyyy}\n" +
                              $"Время: {meeting.StartTime:hh\\:mm} - {meeting.EndTime:hh\\:mm}\n" +
                              $"Цель: {meeting.Purpose}\n" +
                              $"Сумма: {meeting.TotalAmount:C}\n" +
                              $"Статус: {GetStatusDisplay(meeting.Status)}",
                              "Детали встречи",
                              MessageBoxButton.OK,
                              MessageBoxImage.Information);
            }
        }

        // Обработчики событий для отзывов
        private void ReviewItem_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
            {
                border.Cursor = Cursors.Hand;
            }
        }

        private void ReviewItem_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
            {
                border.Cursor = Cursors.Arrow;
            }
        }

        private void ReviewItem_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is ReviewDTO review)
            {
                MessageBox.Show($"Отзыв от {review.ClientName}\n\n" +
                              $"Рейтинг: {new string('★', review.Rating)}{new string('☆', 5 - review.Rating)}\n" +
                              $"Дата: {review.CreatedAt:dd.MM.yyyy}\n\n" +
                              $"Комментарий:\n{review.Comment}",
                              "Детали отзыва",
                              MessageBoxButton.OK,
                              MessageBoxImage.Information);
            }
        }

        private string GetStatusDisplay(string status)
        {
            return status switch
            {
                "Pending" => "Ожидает подтверждения",
                "Confirmed" => "Подтверждено",
                "Completed" => "Завершено",
                "Cancelled" => "Отменено",
                "Rejected" => "Отклонено",
                _ => status
            };
        }
    }
}