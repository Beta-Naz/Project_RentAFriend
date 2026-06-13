using RentAFriendApp.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RentAFriendApp.Views.Controls
{
    public partial class BookingCard : UserControl
    {
        public event EventHandler<int>? CardClicked;
        public event EventHandler<int>? MenuClicked;
        public event EventHandler<int>? DoubleClicked;

        public BookingCard()
        {
            InitializeComponent();
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is UpcomingBookingItem booking)
            {
                if (e.ClickCount == 2)
                    DoubleClicked?.Invoke(this, booking.BookingID);
                else
                    CardClicked?.Invoke(this, booking.BookingID);
            }
        }

        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is UpcomingBookingItem booking)
                MenuClicked?.Invoke(this, booking.BookingID);
        }
    }
}