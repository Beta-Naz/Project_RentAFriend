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
        private int? _currentUserId;
        private string? _currentUserRole;
        private string? _currentUserName;
        public static MainWindow? Instanse { get; private set; }
        public MainWindow(Auth authData)
        {
            InitializeComponent();
            Instanse = this;
            if (Application.Current.Properties.Contains("CurrentUserId"))
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
            //switch (_currentUserRole)
            //{
            //    case "Client":
            //        MainFrame.Navigate(new Views.Client.ClientHomePage(_currentUserId));
            //        Title = "RentAFriend - Клиент";
            //        break;
            //    case "Friend":
            //        MainFrame.Navigate(new Views.Friend.FriendHomePage(_currentUserId));
            //        Title = "RentAFriend - Друг";
            //        break;
            //    case "Admin":
            //        MainFrame.Navigate(new Views.Admin.AdminDashboardPage(_currentUserId)); ;
            //        Title = "RentAFriend - Администратор";
            //        break;
            //}
        }

        private void UpdateUserInfo()
        {
            CurrentUserName.Text = _currentUserName;

            string? roleDisplay = null;
            switch (_currentUserRole)
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
                // Очищаем данные пользователя

                ShowLoginWindow();
            }
        }

        private void ShowLoginWindow()
        {
            //var loginWindow = new Views.Auth.LoginWindow();
            //loginWindow.Show();
            //this.Close();
        }

        private void Profile_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            //switch (_currentUserRole)
            //{
            //    case "Client":
            //        MainFrame.Navigate(new Views.Client.ClientHomePage(_currentUserId));
            //        break;
            //    case "Friend":
            //        MainFrame.Navigate(new Views.Friend.FriendHomePage(_currentUserId));
            //        break;
            //    case "Admin":
            //        MainFrame.Navigate(new Views.Admin.AdminDashboardPage(_currentUserId)); ;
            //        break;
            //}
        }
    }
}