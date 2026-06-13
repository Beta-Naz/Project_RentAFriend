using System.Windows;
using System.Windows.Controls;

namespace RentAFriendApp.Views.Admin.Controls
{
    public partial class AdminReviewRow : UserControl
    {
        public event EventHandler<int>? ApproveClicked;
        public event EventHandler<int>? RejectClicked;

        public AdminReviewRow()
        {
            InitializeComponent();
        }

        private int GetReviewId() => (DataContext as dynamic)?.ReviewID ?? 0;

        private void Approve_Click(object sender, RoutedEventArgs e) => ApproveClicked?.Invoke(this, GetReviewId());
        private void Reject_Click(object sender, RoutedEventArgs e) => RejectClicked?.Invoke(this, GetReviewId());
    }
}