using System.Linq;
using System.Windows;
using System.Windows.Controls;
using RentAFriendApp.ViewModels.Admin;

namespace RentAFriendApp.Views.Admin
{
    public partial class AdminDashboardPage : Page
    {
        private readonly AdminDashboardViewModel _viewModel;

        public AdminDashboardPage(string token)
        {
            InitializeComponent();
            _viewModel = new AdminDashboardViewModel(token);
            DataContext = _viewModel;

            _viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(_viewModel.IsBusy))
                    LoadingOverlay.Visibility = _viewModel.IsBusy ? Visibility.Visible : Visibility.Collapsed;
                else if (e.PropertyName == nameof(_viewModel.HasError))
                {
                    ErrorPanel.Visibility = _viewModel.HasError ? Visibility.Visible : Visibility.Collapsed;
                    ErrorMessageText.Text = _viewModel.ErrorMessage;
                }
            };
        }

        // ===== ЗАКРЫТИЕ ОШИБКИ =====
        private void CloseErrorButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.ClearErrors();
            ErrorPanel.Visibility = Visibility.Collapsed;
        }

        // ===== ПОЛЬЗОВАТЕЛИ =====
        private void UserRow_BlockClicked(object sender, int userId)
        {
            var user = _viewModel.AllUsers.FirstOrDefault(u => u.UserID == userId);
            if (user != null) _viewModel.BlockUserCommand.Execute(user);
        }

        private void UserRow_UnblockClicked(object sender, int userId)
        {
            var user = _viewModel.AllUsers.FirstOrDefault(u => u.UserID == userId);
            if (user != null) _viewModel.UnblockUserCommand.Execute(user);
        }

        private void UserRow_DeleteClicked(object sender, int userId)
        {
            var user = _viewModel.AllUsers.FirstOrDefault(u => u.UserID == userId);
            if (user != null) _viewModel.DeleteUserCommand.Execute(user);
        }

        // ===== ДРУЗЬЯ =====
        private void FriendRow_VerifyClicked(object sender, int profileId)
        {
            var profile = _viewModel.FriendProfiles.FirstOrDefault(p => p.ProfileID == profileId);
            if (profile != null) _viewModel.VerifyFriendCommand.Execute(profile);
        }

        private void FriendRow_RejectClicked(object sender, int profileId)
        {
            var profile = _viewModel.FriendProfiles.FirstOrDefault(p => p.ProfileID == profileId);
            if (profile != null) _viewModel.RejectFriendCommand.Execute(profile);
        }

        // ===== ОТЗЫВЫ =====
        private void ReviewRow_ApproveClicked(object sender, int reviewId)
        {
            _viewModel.ApproveReviewCommand.Execute(reviewId);
        }

        private void ReviewRow_RejectClicked(object sender, int reviewId)
        {
            var reason = Microsoft.VisualBasic.Interaction.InputBox(
                "Укажите причину отклонения:", "Отклонение отзыва", "");

            if (string.IsNullOrWhiteSpace(reason))
            {
                MessageBox.Show("Необходимо указать причину отклонения", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _viewModel.RejectReviewCommand.Execute((reviewId, reason));
        }

        // ===== СИСТЕМА =====
        private void RestartDbButton_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Перезапустить подключение к БД?", "Опасно!",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                MessageBox.Show("БД перезапущена", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BackupButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Резервное копирование запущено...", "Бэкап", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}