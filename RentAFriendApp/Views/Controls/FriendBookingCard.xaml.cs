using System.Windows;
using System.Windows.Controls;

namespace RentAFriendApp.Views.Controls
{
    /// <summary>
    /// Логика взаимодействия для FriendBookingCard.xaml
    /// </summary>
    public partial class FriendBookingCard : UserControl
    {
        public event EventHandler<int>? AcceptClicked;
        public event EventHandler<int>? RejectClicked;
        public event EventHandler<int>? CompleteClicked;
        public event EventHandler<int>? ChatClicked;

        public FriendBookingCard()
        {
            InitializeComponent();
        }

        private int GetBookingId() => (DataContext as dynamic)?.BookingID ?? 0;

        private void BtnAccept_Click(object sender, RoutedEventArgs e) => AcceptClicked?.Invoke(this, GetBookingId());
        private void BtnReject_Click(object sender, RoutedEventArgs e) => RejectClicked?.Invoke(this, GetBookingId());
        private void BtnComplete_Click(object sender, RoutedEventArgs e) => CompleteClicked?.Invoke(this, GetBookingId());
        private void BtnChat_Click(object sender, RoutedEventArgs e) => ChatClicked?.Invoke(this, GetBookingId());
    }
}
