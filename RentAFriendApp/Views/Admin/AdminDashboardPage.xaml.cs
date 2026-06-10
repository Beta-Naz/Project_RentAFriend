// AdminDashboardPage.xaml.cs
using RentAFriendApp.ViewModels.Admin;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace RentAFriendApp.Views.Admin
{
    public partial class AdminDashboardPage : Page
    {
        public AdminDashboardPage(string token)
        {
            InitializeComponent();

            // Создаем ViewModel и передаем ID админа
            var viewModel = new AdminDashboardViewModel(token);
            this.DataContext = viewModel;

            // Подписываемся на события
            viewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var viewModel = sender as ViewModels.Admin.AdminDashboardViewModel;

            if (e.PropertyName == nameof(viewModel.IsBusy))
            {
                LoadingOverlay.Visibility = viewModel.IsBusy ? Visibility.Visible : Visibility.Collapsed;
            }
            else if (e.PropertyName == nameof(viewModel.HasError))
            {
                if (viewModel.HasError)
                {
                    ErrorMessageText.Text = viewModel.ErrorMessage;
                    ErrorPanel.Visibility = Visibility.Visible;
                }
                else
                {
                    ErrorPanel.Visibility = Visibility.Collapsed;
                }
            }
        }

        #region Обработчики кнопок

        private void ViewUserButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is int userId)
            {
                MessageBox.Show($"Просмотр пользователя ID: {userId}",
                    "Просмотр", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void EditUserButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is int userId)
            {
                MessageBox.Show($"Редактирование пользователя ID: {userId}",
                    "Редактирование", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void DetailsUserButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is int userId)
            {
                MessageBox.Show($"Детали пользователя ID: {userId}",
                    "Детали", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ViewFriendButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is int profileId)
            {
                MessageBox.Show($"Просмотр профиля друга ID: {profileId}",
                    "Просмотр профиля", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void RestartDbButton_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Перезапустить подключение к базе данных?",
                "Перезапуск БД", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                MessageBox.Show("Подключение к БД перезапущено", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BackupButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Резервное копирование запущено...",
                "Резервное копирование", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void CloseErrorButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorPanel.Visibility = Visibility.Collapsed;
            var viewModel = DataContext as ViewModels.Admin.AdminDashboardViewModel;
            viewModel?.ClearErrors();
        }

        #endregion
    }
}