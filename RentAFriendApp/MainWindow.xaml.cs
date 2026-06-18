using RentAFriendApp.Context;
using RentAFriendApp.Models;
using System.Windows;
using System.Windows.Controls;

namespace RentAFriendApp
{
    public partial class MainWindow : Window
    {
        private Views.Controls.NotificationPanel? _notificationPanelControl;
        private bool _isNotificationPanelOpen = false;
        public static MainWindow? Instanse { get; private set; }
        private Auth _currentData;

        public MainWindow(Auth authData)
        {
            InitializeComponent();
            Instanse = this;
            _currentData = authData;

            var notifPanel = new Views.Controls.NotificationPanel(_currentData?.Token ?? "");
            notifPanel.OnCloseRequested += NotificationPanel_CloseRequested;
            notifPanel.OnUnreadCountChanged += NotificationPanel_UnreadCountChanged;
            _notificationPanelControl = notifPanel;

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
                    MainFrame.Navigate(new Views.Admin.AdminDashboardPage(_currentData.Token));
                    Title = "RentAFriend - Администратор";
                    break;
            }
            _ = UpdateBadgeAsync();
        }

        private void UpdateUserInfo()
        {
            CurrentUserName.Text = _currentData.FullName;

            string roleDisplay = _currentData.Role switch
            {
                "Client" => "Клиент",
                "Friend" => "Друг",
                "Admin" => "Администратор",
                _ => "Пользователь"
            };
            CurrentUserRole.Text = roleDisplay;
        }

        private void ShowLoginWindow()
        {
            var loginWindow = new Views.AuthSign.LoginWindow();
            loginWindow.Show();
            this.Close();
        }

        public void Logout()
        {
            _ = UserContext.Logout(_currentData.Token);
            ShowLoginWindow();
        }

        // Клик по аватарке
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
                    MainFrame.Navigate(new Views.Admin.AdminDashboardPage(_currentData.Token));
                    break;
            }
        }

        // ========== Бургер-меню ==========

        private void BtnBurger_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            BurgerMenuPopup.IsOpen = !BurgerMenuPopup.IsOpen;
        }
        private void BtnNotifications_Click(object sender, RoutedEventArgs e)
        {
            BurgerMenuPopup.IsOpen = false;

            if (NotificationPopup.Child == null && _notificationPanelControl != null)
            {
                NotificationPopup.Child = _notificationPanelControl;
            }

            NotificationPopup.IsOpen = !NotificationPopup.IsOpen;
            _isNotificationPanelOpen = NotificationPopup.IsOpen;

            if (_isNotificationPanelOpen)
            {
                _ = UpdateBadgeAsync();
            }
        }

        private void BtnMenuLogout_Click(object sender, RoutedEventArgs e)
        {
            BurgerMenuPopup.IsOpen = false;
            var result = MessageBox.Show("Вы уверены, что хотите выйти?", "Выход",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                Logout();
            }
        }

        private void NotificationPanel_CloseRequested()
        {
            NotificationPopup.IsOpen = false;
            _isNotificationPanelOpen = false;
            _ = UpdateBadgeAsync();
        }

        private void NotificationPanel_UnreadCountChanged(int count)
        {
            Dispatcher.Invoke(() =>
            {
                if (count > 0)
                {
                    NotificationBadge.Visibility = Visibility.Visible;
                    BadgeCount.Text = count > 99 ? "99+" : count.ToString();
                }
                else
                {
                    NotificationBadge.Visibility = Visibility.Collapsed;
                }
            });
        }

        private async Task UpdateBadgeAsync()
        {
            try
            {
                var response = await NotificationContext.GetUnreadCount(_currentData.Token);
                if (response != null)
                {
                    NotificationPanel_UnreadCountChanged(response.UnreadCount);
                }
            }
            catch { }
        }
    }
}