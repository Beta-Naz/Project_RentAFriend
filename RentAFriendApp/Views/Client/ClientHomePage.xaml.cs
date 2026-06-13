using RentAFriendApp.Models.ClassesDTO.FriendProfileDTO;
using RentAFriendApp.ViewModels.Client;
using RentAFriendApp.Views.Controls;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace RentAFriendApp.Views.Client
{
    public partial class ClientHomePage : Page
    {
        private readonly ClientHomeViewModel _viewModel;
        private BookingCard? _selectedBookingCard;
        private Border? _selectedFriendCard;

        public ClientHomePage(string token)
        {
            InitializeComponent();

            _viewModel = new ClientHomeViewModel(token);
            DataContext = _viewModel;
        }
        private void StatsCard_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Border card)
            {
                card.Effect = new DropShadowEffect
                {
                    BlurRadius = 20,
                    Opacity = 0.15,
                    ShadowDepth = 3
                };
            }
        }

        private void StatsCard_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Border card)
            {
                var style = (Style)FindResource("StatsCardStyle");
                var effectSetter = style.Setters.OfType<Setter>()
                    .FirstOrDefault(s => s.Property == Border.EffectProperty);
                card.Effect = effectSetter?.Value as DropShadowEffect;
            }
        }

        private void StatsCard_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border card && e.ChangedButton == MouseButton.Left)
            {
                _viewModel.ShowDetailedStatisticsCommand.Execute(card.Tag as string);
            }
        }
        private void BookingCard_CardClicked(object sender, int bookingId)
        {
            if (sender is BookingCard card)
            {
                // Сброс выделения предыдущей
                if (_selectedBookingCard != null && _selectedBookingCard != card)
                {
                    _selectedBookingCard.Background = Brushes.White;
                }

                // Выделение текущей
                card.Background = new SolidColorBrush(Color.FromArgb(30, 76, 175, 80));
                _selectedBookingCard = card;
            }
        }

        private void BookingCard_MenuClicked(object sender, int bookingId)
        {
            var contextMenu = new ContextMenu();

            var detailsItem = new MenuItem { Header = "📋 Детали встречи" };
            detailsItem.Click += (s, args) =>
                _viewModel.ViewBookingDetailsCommand.Execute(bookingId);
            contextMenu.Items.Add(detailsItem);

            var cancelItem = new MenuItem { Header = "❌ Отменить встречу" };
            cancelItem.Click += (s, args) =>
                _viewModel.CancelBookingCommand.Execute(bookingId);
            contextMenu.Items.Add(cancelItem);

            contextMenu.IsOpen = true;
        }

        private void BookingCard_DoubleClicked(object sender, int bookingId)
        {
            _viewModel.ViewBookingDetailsCommand.Execute(bookingId);
        }

        private void FriendCard_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Border card || card.DataContext is not FPInfoDTO friend)
                return;

            if (e.ClickCount == 2)
            {
                _viewModel.OpenFriendProfileCommand.Execute(friend.ProfileID);
                return;
            }

            if (_selectedFriendCard != null)
                _selectedFriendCard.Background = Brushes.White;

            card.Background = new SolidColorBrush(Color.FromArgb(30, 76, 175, 80));
            _selectedFriendCard = card;
        }


        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            var fadeIn = new System.Windows.Media.Animation.DoubleAnimation(0, 1,
                TimeSpan.FromSeconds(0.3));
            BeginAnimation(OpacityProperty, fadeIn);
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            _viewModel.Dispose();
        }
    }
}