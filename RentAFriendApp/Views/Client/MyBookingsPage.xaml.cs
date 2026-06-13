using RentAFriendApp.ViewModels.Client;
using RentAFriendApp.Views.Controls;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace RentAFriendApp.Views.Client
{
    public partial class MyBookingsPage : Page
    {
        private readonly MyBookingsViewModel _viewModel;
        private readonly DispatcherTimer _searchTimer;
        private MyBookingCard? _selectedCard; 
        public MyBookingsPage(string token)
        {
            InitializeComponent();
            _viewModel = new MyBookingsViewModel(token);
            DataContext = _viewModel;
            _searchTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
            _searchTimer.Tick += (_, _) =>
            {
                _searchTimer.Stop();
                _viewModel.SearchCommand.Execute(SearchTextBox.Text);
            };
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.25));
            BeginAnimation(OpacityProperty, fadeIn);
        }

        private void FilterButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button clicked) return;

            var parent = clicked.Parent as Panel;
            if (parent == null) return;

            foreach (var child in parent.Children)
            {
                if (child is Button btn)
                {
                    btn.Style = btn == clicked
                        ? (Style)FindResource("FilterButtonActive")
                        : (Style)FindResource("FilterButton");
                }
            }

            _viewModel.FilterCommand.Execute(clicked.Tag?.ToString() ?? "All");
        }

        // ===== ПОИСК =====
        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _searchTimer.Stop();
            _searchTimer.Start();
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = "";
            _viewModel.SearchCommand.Execute("");
            foreach (var child in FindVisualChildren<Button>(this))
            {
                if (child.Tag?.ToString() == "All")
                    child.Style = (Style)FindResource("FilterButtonActive");
                else if (child.Tag is string tag &&
                         (tag == "Pending" || tag == "Confirmed" || tag == "Completed" || tag == "Cancelled"))
                    child.Style = (Style)FindResource("FilterButton");
            }

            _viewModel.FilterCommand.Execute("All");
            _viewModel.RefreshCommand.Execute(null);
        }

        private void BookingCard_CardSelected(object sender, int bookingId)
        {
            if (sender is MyBookingCard card)
            {
                _selectedCard?.SetSelected(false);
                card.SetSelected(true);
                _selectedCard = card;
            }
        }
        private void BookingCard_ChatClicked(object sender, int friendProfileId)
        {
            _viewModel.OpenChatCommand.Execute(friendProfileId);
        }

        private void BookingCard_CancelClicked(object sender, int bookingId)
        {
            _viewModel.CancelBookingCommand.Execute(bookingId);
        }

        private void BookingCard_ReviewClicked(object sender, int bookingId)
        {
            _viewModel.AddReviewCommand.Execute(bookingId);
        }

        private void BookingCard_PayClicked(object sender, int bookingId)
        {
            _viewModel.ProcessPaymentCommand.Execute(bookingId);
        }
        private void BookingCard_CardDoubleClicked(object sender, int bookingId)
        {
            var booking = _viewModel.FilteredBookings.FirstOrDefault(b => b.BookingID == bookingId);
            if (booking != null) _viewModel.OpenChatCommand.Execute(booking.FriendProfileID);
        }
        private void BtnFindFriends_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.Instanse?.MainFrame.Navigate(new CatalogPage(_viewModel.Token));
        }

        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typed)
                    yield return typed;

                foreach (var nested in FindVisualChildren<T>(child))
                    yield return nested;
            }
        }
    }
}