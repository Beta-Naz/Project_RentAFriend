using System.Windows;
using System.Windows.Controls;

namespace RentAFriendApp.Views.Admin.Controls
{
    public partial class AdminFriendRow : UserControl
    {
        public event EventHandler<int>? VerifyClicked;
        public event EventHandler<int>? RejectClicked;

        public AdminFriendRow()
        {
            InitializeComponent();
        }

        private int GetProfileId() => (DataContext as dynamic)?.ProfileID ?? 0;

        private void Verify_Click(object sender, RoutedEventArgs e) => VerifyClicked?.Invoke(this, GetProfileId());
        private void Reject_Click(object sender, RoutedEventArgs e) => RejectClicked?.Invoke(this, GetProfileId());
    }
}