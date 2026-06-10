using RentAFriendApp.Context;
using RentAFriendApp.Models;
using System.Windows;
using System.Windows.Controls;

namespace RentAFriendApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static MainWindow? Instanse { get; private set; }
        private Auth _currentData;
        public MainWindow(Auth authData)
        {
            InitializeComponent();
            Instanse = this;
            _currentData = authData;
            if (authData != null)
            {
                SetupNavigationByRole();
                UpdateUserInfo();
            }
            else
            {
                ShowLoginWindow();
            }
        }

        private void SetupNavigationByRole()
        {
            switch (_currentData.Role)
            {
                case "Client":
                    MainFrame.Navigate(new Views.Client.ClientHomePage(_currentData.Token));
                    Title = "RentAFriend - Клиент";
                    break;
                case "Friend":
                    MainFrame.Navigate(new Views.Friend.FriendHomePage(_currentData.Token));
                    Title = "RentAFriend - Друг";
                    break;
                case "Admin":
                    MainFrame.Navigate(new Views.Admin.AdminDashboardPage(_currentData.Token)); ;
                    Title = "RentAFriend - Администратор";
                    break;
            }
        }

        private void UpdateUserInfo()
        {
            CurrentUserName.Text = _currentData.FullName;

            string? roleDisplay = null;
            switch (_currentData.Role)
            {
                case "Client": roleDisplay = "Клиент"; break;
                case "Friend": roleDisplay = "Друг"; break;
                case "Admin": roleDisplay = "Администратор"; break;
            }
            if (string.IsNullOrEmpty(roleDisplay))
            {
                roleDisplay = "Пользователь";
            }
            CurrentUserRole.Text = roleDisplay;
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Вы уверены, что хотите выйти?", "Выход",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                Logout();
            }
        }

        private void ShowLoginWindow()
        {
            var loginWindow = new Views.AuthSign.LoginWindow();
            loginWindow.Show();
            this.Close();
        }

        private void Profile_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            switch (_currentData.Role)
            {
                case "Client":
                    MainFrame.Navigate(new Views.Client.ClientHomePage(_currentData.Token));
                    break;
                case "Friend":
                    MainFrame.Navigate(new Views.Friend.FriendHomePage(_currentData.Token));
                    break;
                case "Admin":
                    MainFrame.Navigate(new Views.Admin.AdminDashboardPage(_currentData.Token)); ;
                    break;
            }
        }
        public void Logout()
        {
            _ = UserContext.Logout(_currentData.Token);
            ShowLoginWindow();
        }
    }
}