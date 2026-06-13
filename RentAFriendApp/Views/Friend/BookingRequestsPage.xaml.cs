using RentAFriendApp.ViewModels.Friend;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace RentAFriendApp.Views.Friend
{
    public partial class BookingRequestsPage : Page
    {
        private readonly FriendBookingsViewModel _viewModel;

        public BookingRequestsPage(string token)
        {
            InitializeComponent();
            _viewModel = new FriendBookingsViewModel(token);
            DataContext = _viewModel;
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

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            // Сброс фильтра на "Все"
            var parent = (sender as Button)?.Parent as Panel;
            if (parent != null)
            {
                foreach (var child in FindVisualChildren<Button>(parent))
                {
                    child.Style = child.Tag?.ToString() == "All"
                        ? (Style)FindResource("FilterButtonActive")
                        : (Style)FindResource("FilterButton");
                }
            }

            _viewModel.FilterCommand.Execute("All");
            _viewModel.RefreshCommand.Execute(null);
        }

        private void Card_AcceptClicked(object sender, int bookingId) => _viewModel.AcceptCommand.Execute(bookingId);
        private void Card_RejectClicked(object sender, int bookingId) => _viewModel.RejectCommand.Execute(bookingId);
        private void Card_CompleteClicked(object sender, int bookingId) => _viewModel.CompleteCommand.Execute(bookingId);
        private void Card_ChatClicked(object sender, int bookingId) => _viewModel.ChatCommand.Execute(bookingId);

        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typed) yield return typed;
                foreach (var nested in FindVisualChildren<T>(child)) yield return nested;
            }
        }
    }
}