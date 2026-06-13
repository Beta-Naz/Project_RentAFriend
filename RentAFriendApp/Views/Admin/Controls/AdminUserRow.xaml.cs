using System.Windows;
using System.Windows.Controls;

namespace RentAFriendApp.Views.Admin.Controls
{
    public partial class AdminUserRow : UserControl
    {
        public event EventHandler<int>? BlockClicked;
        public event EventHandler<int>? UnblockClicked;
        public event EventHandler<int>? DeleteClicked;

        public AdminUserRow()
        {
            InitializeComponent();
        }

        private int GetUserId() => (DataContext as dynamic)?.UserID ?? 0;

        private void Block_Click(object sender, RoutedEventArgs e) => BlockClicked?.Invoke(this, GetUserId());
        private void Unblock_Click(object sender, RoutedEventArgs e) => UnblockClicked?.Invoke(this, GetUserId());
        private void Delete_Click(object sender, RoutedEventArgs e) => DeleteClicked?.Invoke(this, GetUserId());
    }
}