using System.Windows;
using System.Windows.Controls;

namespace RentAFriendApp.Views.Admin
{
    public partial class AdminDashboardPage : Page
    {
        private ViewModels.Admin.AdminDashboardViewModel _viewModel;

        public AdminDashboardPage(string token)
        {
            InitializeComponent();
            _viewModel = new ViewModels.Admin.AdminDashboardViewModel(token);
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

        private void CloseErrorButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.ClearErrors();
            ErrorPanel.Visibility = Visibility.Collapsed;
        }

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