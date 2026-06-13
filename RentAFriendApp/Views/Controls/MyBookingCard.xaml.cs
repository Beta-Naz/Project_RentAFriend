using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace RentAFriendApp.Views.Controls
{
    public partial class MyBookingCard : UserControl
    {
        public event EventHandler<int>? ChatClicked;
        public event EventHandler<int>? CancelClicked;
        public event EventHandler<int>? ReviewClicked;
        public event EventHandler<int>? PayClicked;
        public event EventHandler<int>? CardSelected;
        public event EventHandler<int>? CardDoubleClicked;

        public MyBookingCard()
        {
            InitializeComponent();
        }

        public void SetSelected(bool selected)
        {
            RootBorder.BorderBrush = selected
                ? new SolidColorBrush(Color.FromArgb(80, 76, 175, 80))
                : new SolidColorBrush(Colors.Transparent);
        }

        private int GetBookingId() => (DataContext as dynamic)?.BookingID ?? 0;
        private int GetFriendProfileId() => (DataContext as dynamic)?.FriendProfileID ?? 0;

        private void RootBorder_MouseDown(object sender, MouseButtonEventArgs e)
        {
            int id = GetBookingId();
            if (id == 0) return;

            if (e.ClickCount == 2)
                CardDoubleClicked?.Invoke(this, id);
            else
                CardSelected?.Invoke(this, id);
        }

        private void BtnChat_Click(object sender, RoutedEventArgs e) => ChatClicked?.Invoke(this, GetFriendProfileId());
        private void BtnCancel_Click(object sender, RoutedEventArgs e) => CancelClicked?.Invoke(this, GetBookingId());
        private void BtnReview_Click(object sender, RoutedEventArgs e) => ReviewClicked?.Invoke(this, GetBookingId());
        private void BtnPay_Click(object sender, RoutedEventArgs e) => PayClicked?.Invoke(this, GetBookingId());
    }
}