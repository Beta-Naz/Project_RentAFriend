using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using RentAFriendApp.Models;
using RentAFriendApp.Models.ClassesDTO.ReviewDTO;
using RentAFriendApp.ViewModels.Friend;

namespace RentAFriendApp.Views.Friend
{
    public partial class FriendHomePage : Page
    {
        private readonly FriendHomeViewModel _viewModel;

        public FriendHomePage(string token)
        {
            InitializeComponent();
            _viewModel = new FriendHomeViewModel(token);
            DataContext = _viewModel;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // Данные уже загружаются в конструкторе ViewModel
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e) { }

        private void MeetingItem_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is UpcomingMeetingItem m)
            {
                MessageBox.Show(
                    $"Детали встречи:\n\n" +
                    $"Клиент: {m.ClientName}\n" +
                    $"Дата: {m.ScheduleDate:dd.MM.yyyy}\n" +
                    $"Время: {m.StartTime:hh\\:mm} – {m.EndTime:hh\\:mm}\n" +
                    $"Цель: {m.Purpose}\n" +
                    $"Сумма: {m.TotalAmount:N0} ₽\n" +
                    $"Статус: {GetStatusDisplay(m.Status)}",
                    "Детали встречи", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ReviewItem_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is ReviewDTO r)
            {
                var stars = new string('★', r.Rating) + new string('☆', 5 - r.Rating);
                MessageBox.Show(
                    $"Отзыв от {r.ClientName}\n\n" +
                    $"Рейтинг: {stars}\n" +
                    $"Дата: {r.CreatedAt:dd.MM.yyyy}\n\n" +
                    $"Комментарий:\n{r.Comment}",
                    "Детали отзыва", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private string GetStatusDisplay(string status) => status switch
        {
            "Pending" => "Ожидает",
            "Confirmed" => "Подтверждено",
            "Completed" => "Завершено",
            "Cancelled" => "Отменено",
            "Rejected" => "Отклонено",
            _ => status
        };
    }
}